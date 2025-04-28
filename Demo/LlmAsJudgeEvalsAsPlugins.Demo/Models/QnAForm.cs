namespace LlmAsJudgeEvalsAsPlugins.Demo.Models;

public class QnAForm
{
    public string SystemPrompt { get; set; } = "";
    public string AltSystemPrompt { get; set; } = "";
    public string AnswerModel { get; set; } = "gpt-4.1-mini";
    public string EvalModel { get; set; } = "gpt-4.1-nano";
    public List<UserInput> UserInputs { get; set; } = [new UserInput("")];
}
public record UserInput(string Input)
{
    public string Input { get; set; } = Input;
}