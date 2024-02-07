using Microsoft.SemanticKernel;
using System.Reflection;

namespace PromptFlowEvalsAsPlugins;

public class EvalService
{
    private static string PluginPath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory(), "Plugins");
    
    public static async Task<ResultScore> ExecuteEval(InputModel inputModel)
    {
        // Or with Azure OAI and/or gpt-3.5-turbo model
        var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion("gpt-4-turbo-preview", "<YOUR_API_KEY>").Build();
        var evalPlugin = kernel.ImportPluginFromPromptDirectory(Path.Combine(PluginPath, "EvalPlugin"), "EvalPlugin");
        var result = await kernel.InvokeAsync(evalPlugin[inputModel.FunctionName], inputModel.RequiredInputs);
        return new ResultScore(inputModel.FunctionName, result);
    }
    public static Dictionary<string, double> AggregateResults(IEnumerable<ResultScore> resultScores)
    {
        return resultScores.GroupBy(r => r.EvalName).ToDictionary(g => g.Key, g => g.Average(r => r.Score));
    }
        
}
public class ResultScore
{
    public string EvalName { get; set; }

    public int Score { get; set; } = -1;

    /// <summary>
    /// Actual response if it could not be converted into a score
    /// </summary>
    public string? Output { get; set; }
   

    /// <summary>
    /// Should be a score from 1-5; -1 Represents an unparsable result
    /// </summary>
    public ResultScore(string name, FunctionResult result)
    {
        EvalName = name;
        var output = result.GetValue<string>()?.Trim();
        if (int.TryParse(output, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
            Output = output;
        }
    }
}