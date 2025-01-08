using System.Text;
using System.Text.Json;
using HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig;

namespace LlmAsJudgeEvalsAsPlugins.Demo;

public class EvalManager(IConfiguration configuration, ILoggerFactory loggerFactory)
{
	private Kernel CreateKernel(string model = "gpt-3.5-turbo")
	{
		var kernelBuilder = Kernel.CreateBuilder();
		kernelBuilder.Services.AddLogging(o => o.AddConsole());
		var kernel = kernelBuilder.AddOpenAIChatCompletion(model, configuration["OpenAI:ApiKey"]!).Build();

		return kernel;
	}
	public async Task<List<ResultScore>> ExecuteEvals(List<IInputModel> inputs, string model = "gpt-3.5-turbo")
	{
		var kernel = CreateKernel(model);
		var evalService = new EvalService(kernel);
		var resultScores = new List<ResultScore>();
		foreach (var input in inputs)
		{
			var result = await evalService.ExecuteEval(input);
			resultScores.Add(result);
		}
		return resultScores;
	}
	public async IAsyncEnumerable<EvalResultDisplay> ExecuteEvalsAsync(List<IInputModel> inputs, string model = "gpt-3.5-turbo", bool addExplain = false)
	{
		var kernel = CreateKernel(model);
		var evalService = new EvalService(kernel);
		foreach (var input in inputs)
		{
			var result = addExplain ? await evalService.ExecuteScorePlusEval(input): await evalService.ExecuteEval(input);
			var question = input.RequiredInputs["question"]!.ToString();
            var hasAnswer = input.RequiredInputs.TryGetValue("answer", out var answer);
			if (input.RequiredInputs.ContainsName("context"))
			{
				var context = input.RequiredInputs["context"]!.ToString();
				yield return new EvalResultDisplay(question!, answer?.ToString() ?? "", result) { Context = context };
			}
			else
			{
				yield return new EvalResultDisplay(question!, answer?.ToString() ?? "", result);
			}
		}
	}
	public async Task<List<string>> GenerateUserQuestions(string topic, int numberOfQ, string model = "gpt-3.5-turbo")
	{
		var kernel = CreateKernel(model);
		var prompt = $"Generate {numberOfQ} different questions about {topic}. Each question should be on its own line. Do not label the lines.";
		var function = KernelFunctionFactory.CreateFromPrompt(prompt, executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 528, Temperature = 1.0, TopP = 1.0 });
		var result = await kernel.InvokeAsync(function);
		return [.. result.GetValue<string>()!.Split("\n")];
	}
	public async Task<List<IInputModel>> CreateNonRagInputModels(string systemPrompt, List<string> userQustions, string model = "gpt-3.5-turbo", bool withExplain = false)
	{
		var kernel = CreateKernel(model);
		var inputs = new List<IInputModel>();
		foreach (var question in userQustions)
		{
			var settings = new OpenAIPromptExecutionSettings { ChatSystemPrompt = systemPrompt };
			var chat = kernel.GetRequiredService<IChatCompletionService>();
			var chatHistory = new ChatHistory(systemPrompt);
			chatHistory.AddUserMessage(question);
			var response = await chat.GetChatMessageContentAsync(chatHistory, settings, kernel);
			var answer = response.Content/*await kernel.InvokePromptAsync<string>(question, new KernelArguments(settings))*/;
			var empathy = withExplain ? InputModel.EmpathyExplainModel(answer, question) : InputModel.EmpathyModel(answer, question);
			inputs.Add(empathy);
			var fluency = withExplain ? InputModel.FluencyExplainModel(answer, question) : InputModel.FluencyModel(answer, question);
			inputs.Add(fluency);
			var noRagIntelligences = withExplain ? InputModel.PerceivedIntelligenceNonRagExplainModel(answer,question) : InputModel.PerceivedIntelligenceNonRagModel(answer, question);
			inputs.Add(noRagIntelligences);
			var coherence = withExplain ? InputModel.CoherenceExplainModel(answer,question) : InputModel.CoherenceModel(answer, question);
			inputs.Add(coherence);
			var helpfulness = withExplain ? InputModel.HelfulnessExplainModel(answer, question) : InputModel.HelpfulnessModel(answer, question);
			inputs.Add(helpfulness);
        }
		return inputs;
	}
	private const string CollectionName = "ragCollection";
	public async Task<List<IInputModel>> CreateRagInputModels(string systemPrompt, List<string> userQuestions,
        string model = "gpt-3.5-turbo", bool withExplain = false)
	{
		if (!systemPrompt.Contains("$context"))
		{
			systemPrompt += "\n\n## Context\n\n {{$context}}";
		}
		var kernel = CreateKernel(model);
		var memory = CreateSemanticMemory();
		var inputs = new List<IInputModel>();
		foreach (var question in userQuestions)
		{
			var context = await SearchContext(question, memory);
			var kernelArgs = new KernelArguments { ["context"] = context };
			var templateEngine = new KernelPromptTemplateFactory();
			var finalSystemPrompt = await templateEngine.Create(new PromptTemplateConfig(systemPrompt)).RenderAsync(kernel, kernelArgs);
			var settings = new OpenAIPromptExecutionSettings { ChatSystemPrompt = finalSystemPrompt, MaxTokens = 256 };
			var chat = kernel.GetRequiredService<IChatCompletionService>();
			var chatHistory = new ChatHistory(finalSystemPrompt);
			chatHistory.AddUserMessage(question);
			var response = await chat.GetChatMessageContentAsync(chatHistory, settings, kernel);
			var answer = response.Content;
			var groundedness = withExplain ? InputModel.GroundednessExplainModel(answer, question, context) : InputModel.GroundednessModel(answer, question, context);
			inputs.Add(groundedness);
            var groundedness2 = withExplain ? InputModel.Groundedness2ExplainModel(answer, question, context) : InputModel.Groundedness2Model(answer, question, context);
			inputs.Add(groundedness2);
            var ragIntelligence = withExplain ? InputModel.PerceivedIntelligenceExplainModel(answer, question, context) : InputModel.PerceivedIntelligenceModel(answer, question, context);
			inputs.Add(ragIntelligence);
            var relevance = withExplain ? InputModel.RelevanceExplainModel(answer, question, context) : InputModel.RelevanceModel(answer, question, context);
			inputs.Add(relevance);
			var retrieval = withExplain ? InputModel.RetrievalExplainModel(question, context) : InputModel.RetrievalModel(question, context);
            inputs.Add(retrieval);
        }
		return inputs;
	}
	private async Task<string> SearchContext(string question, ISemanticTextMemory memory)
	{
		var resultsList = memory.SearchAsync(CollectionName, question, 5, 0.5);
		var sb = new StringBuilder();
		var itemnumber = 1;
		await foreach (var item in resultsList)
		{
			sb.AppendLine($"{itemnumber}. {item.Metadata.Text}");
			itemnumber++;
		}
		return sb.ToString();
	}
	public async Task<(string topic,string shortTopic)> SaveDocumentAndGenerateTopic(string base64File, string filename, FileType fileType, int chunkSize, int overlap)
	{
		_memory = null;
		var chunks = await ReadAndChunkFile(Convert.FromBase64String(base64File), filename, fileType, chunkSize, overlap);
		var memory = CreateSemanticMemory();
		var index = 0;
		var ids = new List<string>();
		var topicBuilder = new StringBuilder();
		foreach (var chunk in chunks)
		{
			if (index < 3) topicBuilder.AppendLine(chunk);
			var id = await memory.SaveInformationAsync(CollectionName, chunk, $"mem_{index++}", filename);
			ids.Add(id);
		}
		var kernel = CreateKernel();
		var topicText = topicBuilder.ToString();
		var topic = await kernel.InvokePromptAsync<string>($"Generate a 2-3 sentance description of the topics covered in the following content.\n\n##Content\n\n{topicText}");
		var shortTopic = await kernel.InvokePromptAsync<string>($"Generate a 2-3 word topic label for the following content.\n\n##Content\n\n{topic}");
#if DEBUG
		var memoryItems = new List<MemoryRecordMetadata>();
		foreach (var id in ids)
		{
			var item = await memory.GetAsync(CollectionName, id, true);
			memoryItems.Add(item.Metadata);
		}
		await File.WriteAllTextAsync("memoryItems.json", JsonSerializer.Serialize(memoryItems, new JsonSerializerOptions { WriteIndented = true }));
#endif
		return (topic!, shortTopic!);
	}
	private ISemanticTextMemory? _memory;
	private ISemanticTextMemory CreateSemanticMemory(string model = "text-embedding-3-small")
	{
		_memory ??= new MemoryBuilder()
			.WithMemoryStore(new VolatileMemoryStore())
			.WithOpenAITextEmbeddingGeneration(model, configuration["OpenAI:ApiKey"])
			.WithLoggerFactory(loggerFactory)
			.Build();
		return _memory;
	}
	private static async Task<List<string>> ReadAndChunkFile(byte[] file, string filename, FileType fileType, int chunkSize, int overlap)
	{
		var sb = new StringBuilder();
		switch (fileType)
		{
			case FileType.Pdf:
				{
					using var document = PdfDocument.Open(file, new ParsingOptions { UseLenientParsing = true });
					foreach (var page in document.GetPages())
					{
						var pageText = page.Text;
						sb.Append(pageText);
					}

					break;
				}
			case FileType.Text:
				{
					var stream = new StreamReader(new MemoryStream(file));
					var text = await stream.ReadToEndAsync();
					sb.Append(text);
					break;
				}
		}

		var textString = sb.ToString();
		var lines = TextChunker.SplitPlainTextLines(textString, 128, StringHelpers.GetTokens);
		var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, chunkSize, overlap, filename, StringHelpers.GetTokens);
		return paragraphs;
	}
	public Dictionary<string, PromptTemplateConfig> TemplateConfigs()
	{
		return Helpers.GetPromptTemplateConfigs();
	}
}
public record EvalResultDisplay(string Question, string Answer, ResultScore ResultScore)
{
	public string? Context { get; set; }
}
public enum FileType { Pdf, Text }