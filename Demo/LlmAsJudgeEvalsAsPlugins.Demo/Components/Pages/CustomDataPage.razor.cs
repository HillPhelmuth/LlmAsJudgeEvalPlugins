using HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;
using LlmAsJudgeEvalsAsPlugins.Demo.Components.EvalInputs;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LlmAsJudgeEvalsAsPlugins.Demo.Models.Helpers;

namespace LlmAsJudgeEvalsAsPlugins.Demo.Components.Pages;
public partial class CustomDataPage
{
    [Inject]
    private NotificationService NotificationService { get; set; } = default!;
    [Inject]
    private EvalManager EvalManager { get; set; } = default!;
    private List<EvalResultDisplay> _evalResults = [];
    private EvalDisplay _evalDisplay;
    private Dictionary<string, double> _standardAggResults = [];
    private Dictionary<string, double> _logProbAggResults = [];
    private List<JsonDocument> _jsonRecords = [];
    
    private bool _showMappingForm;

    private class FileUploadData
    {
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public byte[]? FileBytes { get; set; }
        public const int MaxFileSize = int.MaxValue;
        public List<EvalType> EvalTypes { get; set; } = [];
        public string EvalModel { get; set; } = "gpt-4.1-nano";
    }

    private class PropertyMappingItem
    {
        public string RequiredKey { get; set; } = "";
        public string JsonPropertyName { get; set; } = "";
    }

    private class PropertyMappingForm
    {
        public List<PropertyMappingItem> Mappings { get; set; } = [];
    }

    private FileUploadData _fileUploadData = new();
    private PropertyMappingForm _propertyMappingForm = new();

    private Dictionary<EvalType, string[]> _requiredKeysMap = new()
    {
        { EvalType.GptGroundedness, ["answer", "context", "question"] },
        { EvalType.GptSimilarity, ["answer", "ground_truth", "question"] },
        { EvalType.Relevance, ["answer", "context", "question"] },
        { EvalType.Coherence, ["answer", "question"] },
        { EvalType.GptGroundedness2, ["answer", "question", "context"] },
        { EvalType.PerceivedIntelligence, ["answer", "question", "context"] },
        { EvalType.PerceivedIntelligenceNonRag, ["answer", "question"] },
        { EvalType.Fluency, ["answer", "question"] },
        { EvalType.Empathy, ["answer", "question"] },
        { EvalType.Helpfulness, ["answer", "question"] },
        { EvalType.Retrieval, ["question", "context"] },
        { EvalType.GptGroundednessExplain, ["answer", "context", "question"] },
        { EvalType.GptGroundedness2Explain, ["answer", "question", "context"] },
        { EvalType.GptSimilarityExplain, ["answer", "ground_truth", "question"] },
        { EvalType.RelevanceExplain, ["answer", "context", "question"] },
        { EvalType.CoherenceExplain, ["answer", "question"] },
        { EvalType.PerceivedIntelligenceExplain, ["answer", "question", "context"] },
        { EvalType.PerceivedIntelligenceNonRagExplain, ["answer", "question"] },
        { EvalType.FluencyExplain, ["answer", "question"] },
        { EvalType.EmpathyExplain, ["answer", "question"] },
        { EvalType.HelpfulnessExplain, ["answer", "question"] },
        { EvalType.RetrievalExplain, ["question", "context"] }
    };

    private HashSet<string> _propertyNames = [];
    private bool _isBusy;

    private HashSet<string> GetUniqueRequiredKeys(IEnumerable<EvalType> evalTypes)
    {
        var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var evalType in evalTypes)
        {
            if (_requiredKeysMap.TryGetValue(evalType, out var keys))
            {
                foreach (var key in keys)
                {
                    uniqueKeys.Add(key);
                }
            }
        }
        return uniqueKeys;
    }

    private Task HandleFile(FileUploadData fileUploadData)
    {
        try
        {
            var data = fileUploadData.FileBytes;
            var initialDataString = System.Text.Encoding.UTF8.GetString(data);
            var base64Data = initialDataString.Split(',')[1];
            var jsonl = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));

            // Parse and store all JSONL records
            _jsonRecords.Clear();
            var lines = jsonl.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                _jsonRecords.Add(JsonDocument.Parse(line));
            }

            if (_jsonRecords.Count == 0)
            {
                NotificationService.Notify(NotificationSeverity.Error, "File Format Error", "No valid JSON records found in file.");
                return Task.CompletedTask;
            }

            // Get property names from first record
            var firstRecord = _jsonRecords[0];
            _propertyNames.Add("");
            
            var propNames = firstRecord.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
            propNames.ToList().ForEach(p => _propertyNames.Add(p));

            // Initialize mappings for unique required keys
            _propertyMappingForm = new PropertyMappingForm();

            var uniqueRequiredKeys = GetUniqueRequiredKeys(fileUploadData.EvalTypes);
            
            foreach (var key in uniqueRequiredKeys)
            {
                // If there's an exact match in the JSON, use it
                var matchedProperty = _propertyNames.FirstOrDefault(p => 
                    string.Equals(p, key, StringComparison.OrdinalIgnoreCase));
                
                _propertyMappingForm.Mappings.Add(new PropertyMappingItem
                {
                    RequiredKey = key,
                    JsonPropertyName = matchedProperty ?? ""
                });
            }

            _fileUploadData = fileUploadData;
            _showMappingForm = true;
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationSeverity.Error, "File Format Error", e.Message);
            Console.WriteLine($"Error: {e.Message}\n");
        }
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task HandleMappingSubmit(PropertyMappingForm mappingForm)
    {
        
        _isBusy = true;
        StateHasChanged();
        await Task.Delay(1);
        try
        {
            var inputs = new List<IInputModel>();
            
            // Create a lookup dictionary for faster property mapping access
            var propertyMappings = mappingForm.Mappings.ToDictionary(
                m => m.RequiredKey,
                m => m.JsonPropertyName,
                StringComparer.OrdinalIgnoreCase
            );
            
            foreach (var jsonDoc in _jsonRecords)
            {
                var root = jsonDoc.RootElement;
                
                foreach (var evalType in _fileUploadData.EvalTypes)
                {
                    if (!_requiredKeysMap.TryGetValue(evalType, out var requiredKeys))
                        continue;

                    // Get values from JSON using mappings
                    var args = new Dictionary<string, string>();
                    foreach (var reqKey in requiredKeys)
                    {
                        if (propertyMappings.TryGetValue(reqKey, out var jsonKey) &&
                            root.TryGetProperty(jsonKey, out var value))
                        {
                            args[reqKey] = value.GetString() ?? "";
                        }
                    }

                    // Create input model based on EvalType using InputModel factory methods
                    var inputModel = evalType.AsInputModel(args);

                    if (inputModel != null)
                    {
                        inputs.Add(inputModel);
                    }
                }
            }

            var index = 1;
            // Execute evaluations using EvalManager
            var scoreOnlyRequests = inputs.Where(x => !x.FunctionName.Contains("Explain")).ToList();
            var cotRequess = inputs.Where(x => x.FunctionName.Contains("Explain")).ToList();
            await foreach (var result in EvalManager.ExecuteEvalsAsync(scoreOnlyRequests, _fileUploadData.EvalModel))
            {
                _evalResults.Add(result);
                await _evalDisplay.RefreshGrid();
                index++;
                if (index % 10 == 0)
                {
                    _standardAggResults = EvalService.AggregateResults(_evalResults.Select(x => x.ResultScore));
                    _logProbAggResults = EvalService.AggregateResults(_evalResults.Select(x => x.ResultScore), true);
                    StateHasChanged();
                }
            }
            await foreach (var result in EvalManager.ExecuteEvalsAsync(cotRequess, _fileUploadData.EvalModel, true))
            {
                _evalResults.Add(result);
                await _evalDisplay.RefreshGrid();
                index++;
                if (index % 10 == 0)
                {
                    _standardAggResults = EvalService.AggregateResults(_evalResults.Select(x => x.ResultScore));
                    _logProbAggResults = EvalService.AggregateResults(_evalResults.Select(x => x.ResultScore), true);
                    StateHasChanged();
                }
            }
            await _evalDisplay.RefreshGrid();
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Evaluation Error", e.Message);
        }
        _standardAggResults = EvalService.AggregateResults(_evalResults.Select(x => x.ResultScore));
        _logProbAggResults = EvalService.AggregateResults(_evalResults.Select(x => x.ResultScore), true);   
#if DEBUG
        var json = JsonSerializer.Serialize(_evalResults, new JsonSerializerOptions() { WriteIndented = true });
        File.WriteAllText("eval_output_finetune.json", json);
#endif
        _isBusy = false;
        StateHasChanged();
    }

    private void HandleError(UploadErrorEventArgs args)
    {
        NotificationService.Notify(NotificationSeverity.Error, "Upload Error", args.Message);
        Console.WriteLine($"Error: {args.Message}\n");
    }
}
