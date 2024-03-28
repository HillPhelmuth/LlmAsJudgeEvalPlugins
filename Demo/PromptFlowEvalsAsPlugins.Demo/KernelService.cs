using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace PromptFlowEvalsAsPlugins.Demo;

public class KernelService
{
    private IConfiguration _configuration;

    public KernelService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private Kernel CreateKernel(string model = "gpt-3.5-turbo")
    {
        var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion(model, _configuration["OpenAI:ApiKey"]!).Build();
        
        return kernel;
    }
    public async Task<List<ResultScore>> ExecuteEvals(List<InputModel> inputs, string model = "gpt-3.5-turbo")
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
    public async IAsyncEnumerable<EvalResultDisplay> ExecuteEvalsAsync(List<InputModel> inputs, string model = "gpt-3.5-turbo")
    {
        var kernel = CreateKernel(model);
        var evalService = new EvalService(kernel);
        foreach (var input in inputs)
        {
            var result = await evalService.ExecuteEval(input);
            var question = input.RequiredInputs["question"]!.ToString();
            var answer = input.RequiredInputs["answer"]!.ToString();
            yield return new EvalResultDisplay(question!, answer!, result);
        }
    }
    public async Task<List<string>> GenerateUserQuestions(string topic, int numberOfQ, string model = "gpt-3.5-turbo")
    {
        var kernel = CreateKernel(model);
        var prompt = $"Generate {numberOfQ} different questions about {topic}. Each question should be on its own line. Do not label the lines.";
        var function = KernelFunctionFactory.CreateFromPrompt(prompt, executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 528, Temperature = 1.2, TopP = 1.0 });
        var result = await kernel.InvokeAsync(function);
        return [.. result.GetValue<string>()!.Split("\n")];
    }
    public async Task<List<InputModel>> CreateNonRagInputModels(string systemPrompt, List<string> userQustions, string model = "gpt-3.5-turbo")
    {
        var kernel = CreateKernel(model);
        var inputs = new List<InputModel>();
        foreach (var question in userQustions)
        {
            var settings = new OpenAIPromptExecutionSettings { ChatSystemPrompt = systemPrompt };
            var answer = await kernel.InvokePromptAsync<string>(question, new KernelArguments(settings));
            var empathy = InputModel.EmpathyModel(answer, question);
            inputs.Add(empathy);
            var fluency = InputModel.FluencyModel(answer, question);
            inputs.Add(fluency);
            var noRagIntelligences = InputModel.PerceivedIntelligenceNonRagModel(answer, question);
            inputs.Add(noRagIntelligences);
            var coherence = InputModel.CoherenceModel(answer, question);
            inputs.Add(coherence);
        }
        return inputs;
    }
}
public record EvalResultDisplay(string Question, string Answer, ResultScore ResultScore);