using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;
using Azure.AI.OpenAI;

namespace PromptFlowEvalsAsPlugins;

public class EvalService
{
	private readonly Kernel _kernel;

	public EvalService(Kernel kernel)
	{
		_kernel = kernel;
	}

	public Dictionary<string, KernelFunction> EvalFunctions { get; } = [];

	public void AddEvalFunction(string name, string prompt, PromptExecutionSettings settings, bool overrideExisting = false)
	{
		var function = KernelFunctionFactory.CreateFromPrompt(prompt, settings, name);
		AddEvalFunction(name, function, overrideExisting);
	}
	public void AddEvalFunction(string name, KernelFunction function, bool overrideExisting = false)
	{
		if (overrideExisting)
			EvalFunctions[name] = function;
		else
			EvalFunctions.TryAdd(name, function);
	}
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
		var logProbs = result.Metadata?["LogProbabilityInfo"] as ChatChoiceLogProbabilityInfo;
		var tokenStrings = logProbs.TokenLogProbabilityResults.AsTokenStrings()[0];
		return new ResultScore(inputModel.FunctionName, tokenStrings);
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
		var logProbs = result.Metadata?["LogProbabilityInfo"] as ChatChoiceLogProbabilityInfo;
		var tokenStrings = logProbs.TokenLogProbabilityResults.AsTokenStrings();
		var scoreResult = result.GetTypedResult<ScorePlusResponse>();
		return new ResultScore(inputModel.FunctionName, scoreResult, tokenStrings);
	}
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
	//public Dictionary<string, double> AggregateResults(IEnumerable<ResultScore> resultScores)
	//{
	//    return resultScores.GroupBy(r => r.EvalName).ToDictionary(g => g.Key, g => g.Where(x => x.Score != -1).Average(r => r.Score));
	//}

}

public class ScorePlusResponse
{
	//[JsonPropertyName("referenceAnswer")]
	//public string? ReferenceAnswer { get; set; }
	[JsonPropertyName("explanation")]
	public string? QualityScoreReasoning { get; set; }
	[JsonPropertyName("score")]
	public int? QualityScore { get; set; }
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