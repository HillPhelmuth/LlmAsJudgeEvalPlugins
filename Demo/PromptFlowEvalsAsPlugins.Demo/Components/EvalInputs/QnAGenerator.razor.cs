using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PromptFlowEvalsAsPlugins.Demo.Components.EvalInputs;

public partial class QnAGenerator : ComponentBase
{
    [Inject]
    private KernelService KernelService { get; set; } = default!;
    [Parameter]
    public EventCallback<EvalResultDisplay> EvalResultGenerated { get; set; }
    [Parameter]
    public EventCallback<Dictionary<string, double>> ResultsAggregated { get; set; }
    private class QnAForm
    {
        public string SystemPrompt { get; set; } = "";
        public List<UserInput> UserInputs { get; set; } = [new UserInput("")];
            
    }
    private bool _isGenerating;
    private bool _isEvaluating;
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
    private async void SubmitInputGen(UserInputGenForm userInputGen)
    {
        _isGenerating = true;
        StateHasChanged();
        await Task.Delay(1);
        foreach (var input in _qnaForm.UserInputs.ToList().Where(x => string.IsNullOrWhiteSpace(x.Input)))
        {
            _qnaForm.UserInputs.Remove(input);
        }
        var inputs = await KernelService.GenerateUserQuestions(userInputGen.Topic, userInputGen.NumQuestions);
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
        var systemPrompt = qnaForm.SystemPrompt;
        var userInputs = qnaForm.UserInputs.Select(ui => ui.Input).ToList();
        var inputModels = await KernelService.CreateNonRagInputModels(systemPrompt, userInputs);
        var results = new List<EvalResultDisplay>();
        await foreach (var result in KernelService.ExecuteEvalsAsync(inputModels))
        {
            results.Add(result);
            await EvalResultGenerated.InvokeAsync(result);
        }
        var aggResults = EvalService.AggregateResults(results.Select(r => r.ResultScore));
        await ResultsAggregated.InvokeAsync(aggResults);
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