using Markdig;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PromptFlowEvalsAsPlugins.Demo.Components.EvalInputs;

public partial class EvalDisplay : ComponentBase
{
    [Parameter]
    public List<EvalResultDisplay> EvalResultDisplays { get; set; } = [];
    [Parameter]
    public Dictionary<string, double> AggregatedResults { get; set; } = [];
    private RadzenDataGrid<EvalResultDisplay>? _grid;
    private string AsHtml(string? text)
    {
        if (text == null) return "";
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var result = Markdown.ToHtml(text, pipeline);
        return result;

    }
    public Task RefreshGrid()
    {
        return _grid?.Reload()!;
    }
}