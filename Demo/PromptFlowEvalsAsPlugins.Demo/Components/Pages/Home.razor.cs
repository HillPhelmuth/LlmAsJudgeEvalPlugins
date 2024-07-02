using PromptFlowEvalsAsPlugins.Demo.Components.EvalInputs;

namespace PromptFlowEvalsAsPlugins.Demo.Components.Pages;

public partial class Home
{
    private List<EvalResultDisplay> _evalResults = [];
    private EvalDisplay _evalDisplay;
    private QnAGenerator _qnAGenerator;
    private Dictionary<string, double> _standardAggResults = [];
    private Dictionary<string, double> _logProbAggResults = [];

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
    private void Reset()
    {
        _evalResults = [];
        _standardAggResults = [];
        _qnAGenerator.Reset();
    }
}