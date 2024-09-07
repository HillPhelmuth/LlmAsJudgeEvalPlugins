using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using OpenAI.Chat;

namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

/// <summary>
/// Represents the evaluation service.
/// </summary>
public class EvalService
{
    private readonly Kernel _kernel;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvalService"/> class.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    public EvalService(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Gets the evaluation functions.
    /// </summary>
    private Dictionary<string, KernelFunction> EvalFunctions { get; } =[];

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
        var function = _kernel.CreateFunctionFromPromptYaml(yamlText);
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
        var function = _kernel.CreateFunctionFromPromptYaml(yamlText);
        AddEvalFunction(name, function, overrideExisting);
    }

    /// <summary>
    /// Executes the evaluation using the specified input model.
    /// </summary>
    /// <param name="inputModel">The input model for the evaluation.</param>
    /// <returns>The result score of the evaluation.</returns>
    public async Task<ResultScore> ExecuteEval(IInputModel inputModel)
    {
        var currentKernel = _kernel.Clone();
        if (currentKernel.Services.GetService<IChatCompletionService>() is null && currentKernel.Services.GetService<ITextGenerationService>() is null)
        {
            throw new Exception("Kernel must have a chat completion service or text generation service to execute an eval");
        }

        var evalPlugin = EvalFunctions.Count == 0 ? currentKernel.ImportEvalPlugin() : KernelPluginFactory.CreateFromFunctions("EvalPlugin", "Evaluation functions", EvalFunctions.Values);
        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1,
            Temperature = 0.1,
            TopP = 0.1,
            ChatSystemPrompt = "You are an AI assistant. You will be given the definition of an evaluation metric for assessing the quality of an answer in a question-answering task. Your job is to compute an accurate evaluation score using the provided evaluation metric. Your response must always be a single numerical value.",
            Logprobs = true,
            TopLogprobs = 5
        };

        var kernelArgs = new KernelArguments(inputModel.RequiredInputs, new Dictionary<string, PromptExecutionSettings> { { "default", settings } });
        var result = await currentKernel.InvokeAsync(evalPlugin[inputModel.FunctionName], kernelArgs);
        var logProbs = result.Metadata?["ContentTokenLogProbabilities"] as IReadOnlyList<ChatTokenLogProbabilityInfo>;
        var tokenStrings = logProbs.AsTokenStrings()[0];
        return new ResultScore(inputModel.FunctionName, tokenStrings);
    }

    /// <summary>
    /// Executes the evaluation with score plus using the specified input model.
    /// </summary>
    /// <param name="inputModel">The input model for the evaluation.</param>
    /// <returns>The result score of the evaluation.</returns>
    public async Task<ResultScore> ExecuteScorePlusEval(IInputModel inputModel)
    {
        var kernel = _kernel.Clone();
        if (kernel.Services.GetService<IChatCompletionService>() is null && kernel.Services.GetService<ITextGenerationService>() is null)
        {
            throw new Exception("Kernel must have a chat completion service or text generation service to execute an eval");
        }
        var evalPlugin = EvalFunctions.Count == 0 ? kernel.ImportEvalPlugin() : KernelPluginFactory.CreateFromFunctions("EvalPlugin", "Evaluation functions", EvalFunctions.Values);
        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 256,
            Temperature = 0.1,
            TopP = 0.1,
            ResponseFormat = "json_object",
            ChatSystemPrompt = "You are an AI assistant. You will be given the definition of an evaluation metric for assessing the quality of an answer in a question-answering task. Your job is to compute an accurate evaluation score using the provided evaluation metric. You must respond in the requested json format",
            Logprobs = true,
            TopLogprobs = 5
        };
        var finalArgs = new KernelArguments(inputModel.RequiredInputs, new Dictionary<string, PromptExecutionSettings> { { PromptExecutionSettings.DefaultServiceId, settings } });
        var result = await kernel.InvokeAsync(evalPlugin[inputModel.FunctionName], finalArgs);
        var logProbs = result.Metadata?["ContentTokenLogProbabilities"] as IReadOnlyList<ChatTokenLogProbabilityInfo>;
        var tokenStrings = logProbs?.AsTokenStrings();
        var scoreResult = result.GetTypedResult<ScorePlusResponse>();
        return new ResultScore(inputModel.FunctionName, scoreResult, tokenStrings);
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
}

/// <summary>
/// Represents the response with score plus information.
/// </summary>
public class ScorePlusResponse
{
    [JsonPropertyName("explanation")]
    public string? QualityScoreReasoning { get; set; }
    [JsonPropertyName("score")]
    public int? QualityScore { get; set; }
}