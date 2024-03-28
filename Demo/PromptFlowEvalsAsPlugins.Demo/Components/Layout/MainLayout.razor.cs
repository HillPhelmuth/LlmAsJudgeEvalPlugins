using PromptFlowEvalsAsPlugins.Demo.Components.EvalInputs;

namespace PromptFlowEvalsAsPlugins.Demo.Components.Layout;

public partial class MainLayout
{
    private List<EvalResultDisplay> _evalResults = [];
    private EvalDisplay _evalDisplay;
    private QnAGenerator _qnAGenerator;
    private Dictionary<string, double> _aggResults = [];

    private async void HandleAddEval(EvalResultDisplay evalResultDisplay)
    {
        _evalResults.Add(evalResultDisplay);
        await _evalDisplay.RefreshGrid();
    }

    private void HandleAggregate(Dictionary<string, double> aggResults)
    {
        _aggResults = aggResults;
    }

    private void Reset()
    {
        _evalResults = [];
        _aggResults = [];
        _qnAGenerator.Reset();
    }
}