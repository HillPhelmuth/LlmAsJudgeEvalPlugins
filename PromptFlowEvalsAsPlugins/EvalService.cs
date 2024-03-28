using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace PromptFlowEvalsAsPlugins;

public class EvalService(Kernel kernel)
{
    public async Task<ResultScore> ExecuteEval(InputModel inputModel)
    {
        var currentKernel = kernel.Clone();
        if(currentKernel.Services.GetService<IChatCompletionService>() is null && currentKernel.Services.GetService<ITextGenerationService>() is null)
        {
            throw new Exception("Kernel must have a chat completion service or text generation service to execute an eval");
        }
        var evalPlugin = currentKernel.ImportEvalPlugin();
        var settings = new OpenAIPromptExecutionSettings { StopSequences = ["stars"], MaxTokens = 128, Temperature = 0.1, TopP = 0.1, ChatSystemPrompt = "You are an AI assistant. You will be given the definition of an evaluation metric for assessing the quality of an answer in a question-answering task. Your job is to compute an accurate evaluation score using the provided evaluation metric." };
        var kernelArgs = new KernelArguments(inputModel.RequiredInputs, new Dictionary<string, PromptExecutionSettings> { { "default", settings} });
        var result = await currentKernel.InvokeAsync(evalPlugin[inputModel.FunctionName], kernelArgs);
        return new ResultScore(inputModel.FunctionName, result);
    }
    public static async Task<ResultScore> ExecuteEval(InputModel inputModel, Kernel evalKernel)
    {
        var kernel = evalKernel.Clone();
        if (kernel.Services.GetService<IChatCompletionService>() is null && kernel.Services.GetService<ITextGenerationService>() is null)
        {
            throw new Exception("Kernel must have a chat completion service or text generation service to execute an eval");
        }
        var evalPlugin = kernel.ImportEvalPlugin();
        var result = await kernel.InvokeAsync(evalPlugin[inputModel.FunctionName], inputModel.RequiredInputs);
        return new ResultScore(inputModel.FunctionName, result);
    }
    public static async Task<ResultScore> ExecuteScorePlusEval(InputModel inputModel, Kernel kernel)
    {
        if (kernel.Services.GetService<IChatCompletionService>() is null && kernel.Services.GetService<ITextGenerationService>() is null)
        {
            throw new Exception("Kernel must have a chat completion service or text generation service to execute an eval");
        }
        var evalPlugin = kernel.ImportEvalPlugin();
        var settings = new OpenAIPromptExecutionSettings { ResponseFormat = "json_object", ChatSystemPrompt = "You must respond in the requested json format" };
        var finalArgs = new KernelArguments(inputModel.RequiredInputs, new Dictionary<string, PromptExecutionSettings> { { PromptExecutionSettings.DefaultServiceId, settings } });
        var result = await kernel.InvokeAsync(evalPlugin[inputModel.FunctionName], finalArgs);
        var scoreResult = result.GetTypedResult<ScorePlusResponse>();
        return new ResultScore(inputModel.FunctionName, scoreResult);
    }
    public static Dictionary<string, double> AggregateResults(IEnumerable<ResultScore> resultScores)
    {
        var result = new Dictionary<string, double>();
        try
        {
            var aggregateResults = resultScores.GroupBy(r => r.EvalName)
                .ToDictionary(g => g.Key, g => g.Average(r => r.Score));
            return aggregateResults;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return result;
        }

    }
    //public Dictionary<string, double> AggregateResults(IEnumerable<ResultScore> resultScores)
    //{
    //    return resultScores.GroupBy(r => r.EvalName).ToDictionary(g => g.Key, g => g.Where(x => x.Score != -1).Average(r => r.Score));
    //}
        
}
public class ResultScore
{
    public string EvalName { get; set; }


    public int Score { get; set; } = -1;

    /// <summary>
    /// Actual response if it could not be converted into a score
    /// </summary>
    public string? Output { get; set; }

    public string? Reasoning { get; set; }
    public string? ReferenceAnswer { get; set; }
    /// <summary>
    /// Should be a score from 1-5; -1 Represents an unparsable result
    /// </summary>
    public ResultScore(string name, FunctionResult result)
    {
        EvalName = name;
        var output = result.GetValue<string>()?.Replace("Score:", "").Trim();
        if (int.TryParse(output, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
            Output = output;
        }
    }
    public ResultScore(string name, string result)
    {
        EvalName = name;
        var output = result.Replace("Score:", "").Trim();
        if (int.TryParse(output, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
            Output = output;
        }
    }
    public ResultScore(string name, ScorePlusResponse? scorePlusResponse)
    {
        EvalName = name;
        var output = scorePlusResponse?.QualityScore?.Replace("Score:", "").Trim();
        if (int.TryParse(output, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
            Output = output;
        }
        Reasoning = scorePlusResponse?.QualityScoreReasoning;
        ReferenceAnswer = scorePlusResponse?.ReferenceAnswer;
    }
}
public class ScorePlusResponse
{
    [JsonPropertyName("referenceAnswer")]
    public string? ReferenceAnswer { get; set; }
    [JsonPropertyName("qualityScoreReasoning")]
    public string? QualityScoreReasoning { get; set; }
    [JsonPropertyName("qualityScore")]
    public string? QualityScore { get; set; }
}
internal class ScorePlusFunctionResult
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    public string Result { get; set; }
    public ScorePlusResponse ScorePlus { get; }
    public FunctionResult FunctionResult { get; }
    public ScorePlusFunctionResult(FunctionResult result)
    {
        FunctionResult = result;
        var resultString = result.GetValue<string>()!;
        Result = resultString;
        var json = resultString.Replace("```json", "").Replace("```", "").Trim();
        ScorePlus = JsonSerializer.Deserialize<ScorePlusResponse>(json, Helpers.JsonOptionsCaseInsensitive)!;

    }
}