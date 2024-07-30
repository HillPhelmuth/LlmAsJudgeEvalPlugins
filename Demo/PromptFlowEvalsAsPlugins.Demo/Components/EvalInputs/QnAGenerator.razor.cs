using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PromptFlowEvalsAsPlugins.Demo.Components.EvalInputs;

public partial class QnAGenerator : ComponentBase
{
    [Inject]
    private EvalManager EvalManager { get; set; } = default!;
    [Parameter]
    public EventCallback<EvalResultDisplay> EvalResultGenerated { get; set; }
    [Parameter]
    public EventCallback<Dictionary<string, double>> StandardResultsAggregated { get; set; }
    [Parameter]
    public EventCallback<Dictionary<string, double>> LogProbResultsAggregrated { get; set; }
    private class QnAForm
    {
        public string SystemPrompt { get; set; } = "";
        public string AnswerModel { get; set; } = "gpt-3.5-turbo";
        public string EvalModel { get; set; } = "gpt-3.5-turbo";
        public List<UserInput> UserInputs { get; set; } = [new UserInput("")];

    }
    private bool _isGenerating;
    private bool _isEvaluating;
    private List<string> _availableModels = ["gpt-3.5-turbo", "gpt-4o-mini", "gpt-4o"];
    private record UserInput(string Input)
    {
        public string Input { get; set; } = Input;
    }

    private QnAForm _qnaForm = new();
    private class UserInputGenForm
    {
        public string Topic { get; set; } = "";
        public int NumQuestions { get; set; } = 10;
    }
    private UserInputGenForm _userInputGenForm = new();

    private void AddInput()
    {

        _qnaForm.UserInputs.Add(new UserInput(""));
        StateHasChanged();
    }
    private void SelectSample(string sampleText)
    {
        _qnaForm.SystemPrompt = sampleText + " Limit your response to 50 words.";
        Console.WriteLine($"SystemPrompt: {_qnaForm.SystemPrompt}");
        StateHasChanged();
    }
    private async void SubmitInputGen(UserInputGenForm userInputGen)
    {
        _isGenerating = true;
        StateHasChanged();
        await Task.Delay(1);
        foreach (var input in _qnaForm.UserInputs.ToList().Where(x => string.IsNullOrWhiteSpace(x.Input)))
        {
            _qnaForm.UserInputs.Remove(input);
        }
        var inputs = await EvalManager.GenerateUserQuestions(userInputGen.Topic, userInputGen.NumQuestions);
        foreach (var input in inputs)
        {
            if (string.IsNullOrWhiteSpace(input)) continue;
            _qnaForm.UserInputs.Add(new UserInput(input));
        }
        _isGenerating = false;
        StateHasChanged();
    }
    private async void SubmitQnA(QnAForm qnaForm)
    {
        _isEvaluating = true;
        StateHasChanged();
        await Task.Delay(1);
        Console.WriteLine($"SystemPrompt At Submit: {_qnaForm.SystemPrompt}");
        var systemPrompt = _qnaForm.SystemPrompt;
        var userInputs = qnaForm.UserInputs.Select(ui => ui.Input).ToList();
        var inputModels = await EvalManager.CreateNonRagInputModels(systemPrompt, userInputs, qnaForm.AnswerModel);
        var results = new List<EvalResultDisplay>();
        await foreach (var result in EvalManager.ExecuteEvalsAsync(inputModels, qnaForm.EvalModel, useLogProbs: true))
        {
            results.Add(result);
            await EvalResultGenerated.InvokeAsync(result);
        }
        var aggResults = EvalService.AggregateResults(results.Select(r => r.ResultScore), true);
        await LogProbResultsAggregrated.InvokeAsync(aggResults);
        var standardResults = EvalService.AggregateResults(results.Select(r => r.ResultScore), false);
        await StandardResultsAggregated.InvokeAsync(standardResults);
        _isEvaluating = false;
        StateHasChanged();
        // Do something with the resultScores
    }
    public void Reset()
    {
        _qnaForm = new QnAForm();
        _userInputGenForm = new UserInputGenForm();
        StateHasChanged();
    }
}