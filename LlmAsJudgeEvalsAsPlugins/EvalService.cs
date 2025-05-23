﻿using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using OpenAI;
using OpenAI.Chat;
#pragma warning disable SKEXP0001

namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

/// <summary>
/// Represents the evaluation service.
/// </summary>
public class EvalService
{
    private const string? ChatSystemPrompt =
            """
            # Instruction
            ## Goal
            ### You are an expert in evaluating the quality of a RESPONSE from an intelligent system based on provided definition and data. Your goal will involve answering the questions below using the information provided.
            - **Definition**: You are given a definition of the communication trait that is being evaluated to help guide your Score.
            - **Tasks**: To complete your evaluation you will be asked to evaluate the Data in different ways.
            """;
    private readonly Kernel _kernel;
    private readonly Func<string, KernelFunction> _createFunctionFromYaml;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvalService"/> class using a <see cref="Kernel"/> instance.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <param name="createFunctionFromYaml">Optional delegate for creating KernelFunction from YAML.</param>
    public EvalService(Kernel kernel, Func<string, KernelFunction>? createFunctionFromYaml = null)
    {
        _kernel = kernel;
        _createFunctionFromYaml = createFunctionFromYaml ?? (yaml => _kernel.CreateFunctionFromPromptYaml(yaml));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvalService"/> class using an <see cref="OpenAIClient"/>.
    /// </summary>
    /// <param name="openAiClient">The OpenAI client to use for chat completion services.</param>
    /// <param name="model">The model to use for evals. Defaults to gpt-4.1-nano</param>
    public EvalService(OpenAIClient openAiClient, string? model = null)
    {
        model ??= "gpt-4.1-nano";
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(new OpenAIChatCompletionService(model, openAiClient));
        _kernel = builder.Build();
        _createFunctionFromYaml = yaml => _kernel.CreateFunctionFromPromptYaml(yaml);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvalService"/> class using an <see cref="IChatClient"/>.
    /// </summary>
    /// <param name="chatClient">The chat client to use for chat completion services.</param>
    public EvalService(IChatClient chatClient)
    {
        var builder = Kernel.CreateBuilder();
        var chatService = chatClient.AsChatCompletionService();
        builder.Services.AddSingleton(chatService);
        _kernel = builder.Build();
        _createFunctionFromYaml = yaml => _kernel.CreateFunctionFromPromptYaml(yaml);
    }

    /// <summary>
    /// Gets the evaluation functions.
    /// </summary>
    private Dictionary<string, KernelFunction> EvalFunctions { get; } = [];


    /// <summary>
    /// Adds an evaluation function using the specified prompt and settings.
    /// </summary>
    /// <param name="name">The name of the evaluation function.</param>
    /// <param name="prompt">The prompt for the evaluation function.</param>
    /// <param name="settings">The execution settings for the evaluation function.</param>
    /// <param name="overrideExisting">Specifies whether to override an existing evaluation function with the same name.</param>
    public void AddEvalFunction(string name, string prompt, PromptExecutionSettings settings, bool overrideExisting = false)
    {
        var function = KernelFunctionFactory.CreateFromPrompt(prompt, settings, name);
        AddEvalFunction(name, function, overrideExisting);
    }

    /// <summary>
    /// Adds an evaluation function.
    /// </summary>
    /// <param name="name">The name of the evaluation function.</param>
    /// <param name="function">The evaluation function.</param>
    /// <param name="overrideExisting">Specifies whether to override an existing evaluation function with the same name.</param>
    public void AddEvalFunction(string name, KernelFunction function, bool overrideExisting = false)
    {
        if (overrideExisting)
            EvalFunctions[name] = function;
        else
            EvalFunctions.TryAdd(name, function);
    }

    /// <summary>
    /// Adds an evaluation function from a YAML stream.
    /// </summary>
    /// <param name="yamlStream">The YAML stream containing the evaluation function.</param>
    /// <param name="name">The name of the evaluation function.</param>
    /// <param name="overrideExisting">Specifies whether to override an existing evaluation function with the same name.</param>
    public void AddEvalFunctionFromYaml(Stream yamlStream, string name, bool overrideExisting = false)
    {
        var yamlText = new StreamReader(yamlStream).ReadToEnd();
        var function = _createFunctionFromYaml(yamlText);
        AddEvalFunction(name, function, overrideExisting);
    }

    /// <summary>
    /// Adds an evaluation function from YAML text.
    /// </summary>
    /// <param name="yamlText">The YAML text containing the evaluation function.</param>
    /// <param name="name">The name of the evaluation function.</param>
    /// <param name="overrideExisting">Specifies whether to override an existing evaluation function with the same name.</param>
    public void AddEvalFunctionFromYaml(string yamlText, string name, bool overrideExisting = false)
    {
        var function = _createFunctionFromYaml(yamlText);
        AddEvalFunction(name, function, overrideExisting);
    }

    /// <summary>
    /// Executes the evaluation using the specified input model.
    /// </summary>
    /// <param name="inputModel">The input model for the evaluation.</param>
    /// <param name="settings">optional <see cref="T:Microsoft.SemanticKernel.PromptExecutionSettings"/>. Defaults to preset <see cref="T:Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings"/></param>
    /// <param name="serviceId">optional service id for keyed <see cref="IChatCompletionService"/></param>
    /// <returns>The result score of the evaluation.</returns>
    public async Task<ResultScore> ExecuteEval(IInputModel inputModel, PromptExecutionSettings? settings = null, string? serviceId = null)
    {
        var currentKernel = _kernel.Clone();
        var missingChatService = currentKernel.Services.GetService<IChatCompletionService>() is null && currentKernel.Services.GetService<ITextGenerationService>() is null;
        if (!string.IsNullOrEmpty(serviceId))
        {
            missingChatService = currentKernel.Services.GetKeyedService<IChatCompletionService>(serviceId) is null && currentKernel.Services.GetKeyedService<ITextGenerationService>(serviceId) is null;
        }
        if (missingChatService)
        {
            throw new Exception("Kernel must have a chat completion service or text generation service to execute an eval");
        }

        var importedEvalPluginFunctions = Helpers.GetFunctionsFromYaml();
        foreach (var importedFunction in importedEvalPluginFunctions.Where(importedFunction => !EvalFunctions.ContainsKey(importedFunction.Key)))
        {
            EvalFunctions.Add(importedFunction.Key, importedFunction.Value);
        }

        var evalPlugin = KernelPluginFactory.CreateFromFunctions("EvalPlugin", "Evaluation functions", EvalFunctions.Values);
        
        settings ??= new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1,
            Temperature = 0.0,
            TopP = 0.0,
            ChatSystemPrompt = ChatSystemPrompt,
            Logprobs = true,
            TopLogprobs = 5,

        };

        var kernelArgs = new KernelArguments(inputModel.RequiredInputs, new Dictionary<string, PromptExecutionSettings> { { PromptExecutionSettings.DefaultServiceId, settings } });
        var result = await currentKernel.InvokeAsync(evalPlugin[inputModel.FunctionName], kernelArgs);
        var logProbs = result.Metadata?["ContentTokenLogProbabilities"] as IReadOnlyList<ChatTokenLogProbabilityDetails>;
        if (logProbs is null || logProbs.Count == 0)
            return new ResultScore(inputModel.FunctionName, result);
        var tokenStrings = logProbs.AsTokenStrings()[0];
        return new ResultScore(inputModel.FunctionName, tokenStrings);
    }

    /// <summary>
    /// Executes the evaluation with score plus a generated explanation using the specified input model.
    /// </summary>
    /// <param name="inputModel">The input model for the evaluation.</param>
    /// <param name="settings">optional <see cref="T:Microsoft.SemanticKernel.PromptExecutionSettings"/>. Defaults to preset <see cref="T:Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings"/></param>
    /// <param name="serviceId">optional service id for keyed <see cref="IChatCompletionService"/></param>
    /// <returns>The result score of the evaluation.</returns>
    public async Task<ResultScore> ExecuteScorePlusEval(IInputModel inputModel, PromptExecutionSettings? settings = null, string? serviceId = null)
    {
        var currentKernel = _kernel.Clone();
        var missingChatService = currentKernel.Services.GetService<IChatCompletionService>() is null && currentKernel.Services.GetService<ITextGenerationService>() is null;
        if (!string.IsNullOrEmpty(serviceId))
        {
            missingChatService = currentKernel.Services.GetKeyedService<IChatCompletionService>(serviceId) is null && currentKernel.Services.GetKeyedService<ITextGenerationService>(serviceId) is null;
        }
        if (missingChatService)
        {
            throw new Exception("Kernel must have a chat completion service or text generation service to execute an eval");
        }


        var importedEvalPluginFunctions = Helpers.GetFunctionsFromYaml();
        foreach (var importedFunction in importedEvalPluginFunctions.Where(importedFunction => !EvalFunctions.ContainsKey(importedFunction.Key)))
        {
            EvalFunctions.Add(importedFunction.Key, importedFunction.Value);
        }

        var evalPlugin = KernelPluginFactory.CreateFromFunctions("EvalPlugin", "Evaluation functions", EvalFunctions.Values);

        settings ??= new OpenAIPromptExecutionSettings
        {
            MaxTokens = 800,
            Temperature = 0.0,
            ResponseFormat = "json_object",
            ChatSystemPrompt = ChatSystemPrompt,
            Logprobs = true,
            TopLogprobs = 5
        };
        var finalArgs = new KernelArguments(inputModel.RequiredInputs, new Dictionary<string, PromptExecutionSettings> { { PromptExecutionSettings.DefaultServiceId, settings } });
        var result = await currentKernel.InvokeAsync(evalPlugin[inputModel.FunctionName], finalArgs);
        var logProbs = result.Metadata?["ContentTokenLogProbabilities"] as IReadOnlyList<ChatTokenLogProbabilityDetails>;
        if (logProbs is null || logProbs.Count == 0)
            return new ResultScore(inputModel.FunctionName, result);
        var tokenStrings = logProbs?.AsTokenStrings();
        var scoreResult = result.GetTypedResult<ScorePlusResponse>();
        return new ResultScore(inputModel.FunctionName, scoreResult, tokenStrings);
    }

    /// <summary>
    /// Executes an evaluation function with a custom output type and score property.
    /// </summary>
    /// <typeparam name="T">The type of the expected result value in the evaluation output.</typeparam>
    /// <param name="inputModel">The input model containing the function name and required arguments for the evaluation.</param>
    /// <param name="scoreProperty">The property name in the result JSON that contains the score.</param>
    /// <param name="settings">
    /// Optional prompt execution settings to use for the evaluation. If null, default settings are used.
    /// </param>
    /// <param name="serviceId">
    /// Optional service ID for selecting a keyed chat or text generation service.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="ResultScore{T}"/> object
    /// with the strongly-typed result value and score extracted from the evaluation output.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown if the kernel does not have a chat completion service or text generation service to execute the evaluation.
    /// </exception>
    public async Task<ResultScore<T>> ExecuteEvalWithCustomOutput<T>(IInputModel inputModel, string scoreProperty,
        PromptExecutionSettings? settings = null, string? serviceId = null)
    {
        var currentKernel = _kernel.Clone();
        var missingChatService = currentKernel.Services.GetService<IChatCompletionService>() is null && currentKernel.Services.GetService<ITextGenerationService>() is null;
        if (!string.IsNullOrEmpty(serviceId))
        {
            missingChatService = currentKernel.Services.GetKeyedService<IChatCompletionService>(serviceId) is null && currentKernel.Services.GetKeyedService<ITextGenerationService>(serviceId) is null;
        }
        if (missingChatService)
        {
            throw new Exception("Kernel must have a chat completion service or text generation service to execute an eval");
        }
        var importedEvalPluginFunctions = Helpers.GetFunctionsFromYaml();
        foreach (var importedFunction in importedEvalPluginFunctions.Where(importedFunction => !EvalFunctions.ContainsKey(importedFunction.Key)))
        {
            EvalFunctions.Add(importedFunction.Key, importedFunction.Value);
        }
        var evalPlugin = KernelPluginFactory.CreateFromFunctions("EvalPlugin", "Evaluation functions", EvalFunctions.Values);
        settings ??= new OpenAIPromptExecutionSettings
        {
            MaxTokens = 800,
            Temperature = 0.0,
            ResponseFormat = "json_object",
            ChatSystemPrompt = ChatSystemPrompt,
            Logprobs = true,
            TopLogprobs = 5
        };
        var kernelArgs = new KernelArguments(inputModel.RequiredInputs, new Dictionary<string, PromptExecutionSettings> { { PromptExecutionSettings.DefaultServiceId, settings } });
        var result = await currentKernel.InvokeAsync(evalPlugin[inputModel.FunctionName], kernelArgs);
        var logProbs = result.Metadata?["ContentTokenLogProbabilities"] as IReadOnlyList<ChatTokenLogProbabilityDetails>;
        if (logProbs is null || logProbs.Count == 0)
            return new ResultScore<T>(inputModel.FunctionName, scoreProperty, result);
        var tokenStrings = logProbs.AsTokenStrings();
        return new ResultScore<T>(inputModel.FunctionName, scoreProperty, tokenStrings);
    }
    /// <summary>
    /// Aggregates the results of multiple result scores.
    /// </summary>
    /// <param name="resultScores">The result scores to aggregate.</param>
    /// <param name="useLogProbs">Specifies whether to use log probabilities for aggregation.</param>
    /// <returns>The aggregated results.</returns>
    public static Dictionary<string, double> AggregateResults(IEnumerable<ResultScore> resultScores, bool useLogProbs = false)
    {
        var result = new Dictionary<string, double>();
        try
        {
            var aggregateResults = resultScores.GroupBy(r => r.EvalName)
                .ToDictionary(g => g.Key, g =>
                {
                    Func<ResultScore, double> selector = useLogProbs ? r => r.ProbScore : r => r.Score;
                    return g.Average(selector);
                });
            return aggregateResults;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return result;
        }
    }
    /// <summary>
    /// Executes multiple evaluation functions asynchronously for the provided input models, then aggregates their results.
    /// </summary>
    /// <param name="inputModels">A collection of input models to evaluate.</param>
    /// <param name="settings">Optional prompt execution settings to use for each evaluation. If null, default settings are used.</param>
    /// <param name="serviceId">Optional service ID for selecting a keyed chat or text generation service.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a dictionary mapping evaluation names to their aggregated scores.
    /// </returns>
    public async Task<Dictionary<string, double>> ExecuteAndAggregateEvals(IEnumerable<IInputModel> inputModels, PromptExecutionSettings? settings = null, string? serviceId = null)
    {
        var resultScores = new List<ResultScore>();
        foreach (var inputModel in inputModels)
        {
            var resultScore = await ExecuteEval(inputModel, settings, serviceId);
            resultScores.Add(resultScore);
        }
        return AggregateResults(resultScores);
    }
}

/// <summary>
/// Represents the response with score plus information.
/// </summary>
public class ScorePlusResponse
{
    /// <summary>
    /// chain of thought for the response
    /// </summary>
    [JsonPropertyName("thoughtChain")]
    public string? ChainOfThought { get; set; }
    /// <summary>
    /// Gets or sets the reasoning behind the quality score.
    /// </summary>
    [JsonPropertyName("explanation")]
    public string? QualityScoreReasoning { get; set; }
    /// <summary>
    /// Gets or sets the quality score.
    /// </summary>
    [JsonPropertyName("score")]
    public int? QualityScore { get; set; }
}