using LlmAsJudgeEvalsAsPlugins.Demo.Components.EvalInputs;
using LlmAsJudgeEvalsAsPlugins.Demo.Components.EvalsRag;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;

namespace LlmAsJudgeEvalsAsPlugins.Demo.Components.Pages;

public partial class Home
{
    [Inject]
    private EvalManager EvalManager { get; set; } = default!;
    private List<EvalResultDisplay> _evalResults = [];
    private EvalDisplay _evalDisplay;
    private QnAGenerator? _qnAGenerator;
    private AddRagContent? _addRagContent;
    private QnAGenerator? _explainQnAGenerator;
    private AddRagContent? _explainAddRagContent;
    private Dictionary<string, double> _standardAggResults = [];
    private Dictionary<string, double> _logProbAggResults = [];
    private Dictionary<string, PromptTemplateConfig> _evalTemplates = [];
    private const string PdfDataUriPrefix = "data:application/pdf;base64,";
    private string _pdfUri = "";
	protected override Task OnInitializedAsync()
	{
        _evalTemplates = EvalManager.TemplateConfigs();
        _pdfUri = $"{PdfDataUriPrefix}{Convert.ToBase64String(FileHelpers.ReadFromAssembly("Driver Guide Latests-VeryShort.pdf"))}#zoom=95";
		return base.OnInitializedAsync();
	}
	private async void HandleAddEval(EvalResultDisplay evalResultDisplay)
    {
        _evalResults.Add(evalResultDisplay);
        await _evalDisplay.RefreshGrid();
    }

    private void HandleAggregate(Dictionary<string, double> aggResults)
    {
        _standardAggResults = aggResults;
    }
    private void HandleLogProbAggregate(Dictionary<string, double> aggResults)
    {
        _logProbAggResults = aggResults;
    }
    private void ClearEvals()
    {
        _evalResults = [];
        _standardAggResults = [];
        _logProbAggResults = [];
        StateHasChanged();
    }
    private void Reset()
    {
        _evalResults = [];
        _standardAggResults = [];
        _logProbAggResults = [];
        _qnAGenerator?.Reset();
        _addRagContent?.Reset();
        _explainQnAGenerator?.Reset();
        _explainAddRagContent?.Reset();
        StateHasChanged();
    }
    private string AsHtml(string? text)
    {
	    if (text == null) return "";
	    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
	    var result = Markdown.ToHtml(text, pipeline);
	    return result;

    }
}