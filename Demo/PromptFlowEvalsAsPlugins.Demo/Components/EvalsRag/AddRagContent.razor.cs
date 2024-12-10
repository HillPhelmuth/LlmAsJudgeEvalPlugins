using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

namespace PromptFlowEvalsAsPlugins.Demo.Components.EvalsRag;

public partial class AddRagContent : ComponentBase
{
	private const string DefaultContentFileName = "Driver Guide Latests-VeryShort.pdf";

	[Inject]
	private EvalManager EvalManager { get; set; } = default!;
	[Parameter]
	public EventCallback<EvalResultDisplay> EvalResultGenerated { get; set; }
	[Parameter]
	public EventCallback<Dictionary<string, double>> StandardResultsAggregated { get; set; }
	[Parameter]
	public EventCallback<Dictionary<string, double>> LogProbResultsAggregrated { get; set; }

	private class FileUpload
	{
		public string? FileName { get; set; }
		public string? FileBase64 { get; set; }
		public long? FileSize { get; set; }
	}
	private class AddContentForm
	{
		public FileUpload FileUpload { get; set; } = new();
		public int ChunckSize { get; set; } = 500;
		public int Overlap { get; set; } = 125;
		public bool UseDefault { get; set; }
	}
	private void DefaultToggleChange(bool value)
	{
		if (value)
		{
			_addContentForm.FileUpload.FileBase64 = Convert.ToBase64String(FileHelpers.ReadFromAssembly(DefaultContentFileName));
			_addContentForm.FileUpload.FileName = DefaultContentFileName;
		}
		else
		{
			_addContentForm.FileUpload.FileBase64 = null;
			_addContentForm.FileUpload.FileName = null;
		}
	}
	private AddContentForm _addContentForm = new();
	public int MaxFileSize => 25 * 1024 * 1024;

	private class QnAForm
	{
		public string SystemPrompt { get; set; } = "";
        public string AnswerModel { get; set; } = "gpt-4o-mini";
        public string EvalModel { get; set; } = "gpt-4o-mini";
        public List<UserInput> UserInputs { get; set; } = [new UserInput("")];

	}
	private bool _isGenerating;
	private bool _isEvaluating;
	private bool _isSaving;
	private record UserInput(string Input)
	{
		public string Input { get; set; } = Input;
	}
    private List<string> _availableModels = ["gpt-3.5-turbo", "gpt-4o-mini"];
    private QnAForm _qnaForm = new();
	private class UserInputGenForm
	{
		public string Topic { get; set; } = "";
		public string ShortTopic { get; set; } = "";
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
		_qnaForm.SystemPrompt = sampleText + " Limit your response to 100 words.\n\n## Context\n\n{{$context}}";
		Console.WriteLine($"SystemPrompt: {_qnaForm.SystemPrompt}");
		StateHasChanged();
	}
	private async void SubmitContent(AddContentForm addContentForm)
	{
		_isSaving = true;
		StateHasChanged();
		await Task.Delay(1);
		if (!addContentForm.UseDefault)
		{
			var fileType = addContentForm.FileUpload.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
				? FileType.Pdf
				: FileType.Text;
			var fileUploadFileBase64Uri = addContentForm.FileUpload.FileBase64;
			var fileBase64 = GetBase64Substring(fileUploadFileBase64Uri!);
			(_userInputGenForm.Topic, _userInputGenForm.ShortTopic) = await EvalManager.SaveDocumentAndGenerateTopic(
				fileBase64, addContentForm.FileUpload.FileName, fileType, addContentForm.ChunckSize,
				addContentForm.Overlap);
		}
		else
		{
			var fileBase64 = Convert.ToBase64String(FileHelpers.ReadFromAssembly(DefaultContentFileName));
			(_userInputGenForm.Topic, _userInputGenForm.ShortTopic) = await EvalManager.SaveDocumentAndGenerateTopic(
				fileBase64, DefaultContentFileName,FileType.Pdf, addContentForm.ChunckSize,
				addContentForm.Overlap);
		}
		_isSaving = false;
		StateHasChanged();
	}
	string GetBase64Substring(string input)
	{
		const string base64Marker = ";base64,";
		int markerIndex = input.IndexOf(base64Marker, StringComparison.Ordinal);
		return markerIndex >= 0 ? input[(markerIndex + base64Marker.Length)..] : string.Empty;
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
		var inputModels = await EvalManager.CreateRagInputModels(systemPrompt, userInputs, qnaForm.AnswerModel);
		var results = new List<EvalResultDisplay>();
		await foreach (var result in EvalManager.ExecuteEvalsAsync(inputModels, qnaForm.EvalModel))
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