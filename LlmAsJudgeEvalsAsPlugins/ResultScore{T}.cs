using System.Text.Json;
using Microsoft.SemanticKernel;

namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

/// <summary>
/// Represents a result score with a strongly-typed result value.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public class ResultScore<T> : ResultScore
{
    /// <summary>
    /// Gets or sets the strongly-typed result value.
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultScore{T}"/> class using the specified evaluation name, score property, and function result.
    /// </summary>
    /// <param name="name">The name of the evaluation.</param>
    /// <param name="scoreProperty">The property name in the result JSON that contains the score.</param>
    /// <param name="result">The function result containing the evaluation data.</param>
    public ResultScore(string name, string scoreProperty, FunctionResult result) : base(name)
    {
        var resultJson = result.ToString();
        
        var jsonDocument = JsonDocument.Parse(resultJson);
        if (jsonDocument.RootElement.TryGetProperty(scoreProperty, out var scoreElement))
        {
            Score = scoreElement.ValueKind switch
            {
                JsonValueKind.Number => scoreElement.GetInt32(),
                JsonValueKind.String when int.TryParse(scoreElement.GetString(), out var parsedScore) => parsedScore,
                _ => Score
            };
        }

        if (resultJson != null)
        {
            Result = JsonSerializer.Deserialize<T>(resultJson, Helpers.JsonOptionsCaseInsensitive);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultScore{T}"/> class using the specified evaluation name, score property, and token strings.
    /// </summary>
    /// <param name="name">The name of the evaluation.</param>
    /// <param name="scoreProperty">The property name in the result JSON that contains the score.</param>
    /// <param name="tokenStrings">The list of <see cref="TokenString"/> objects representing the tokenized JSON result.</param>
    /// <remarks>
    /// This constructor deserializes the JSON result from the provided token strings into the strongly-typed <typeparamref name="T"/> result.
    /// It also attempts to extract the score from the token strings using the specified score property name.
    /// The probability score and log probability results are calculated from the token information if available.
    /// </remarks>
    public ResultScore(string name, string scoreProperty, List<TokenString> tokenStrings) : base(name)
    {
        EvalName = name;
        var fullJson = string.Join("", tokenStrings.Select(x => x.StringValue));
        Result = JsonSerializer.Deserialize<T>(fullJson, Helpers.JsonOptionsCaseInsensitive);
        Console.WriteLine(fullJson);
        var logProb = GetTokenAfterScore(tokenStrings, scoreProperty, StringComparison.OrdinalIgnoreCase);
        var logProbVals = logProb?.TopLogProbs;
        var output = logProb?.StringValue;
        LogProbResults = logProbVals;
        ProbScore = logProbVals?.Select(x => x.AsTokenProb()).CalculateWeightedScore() ?? -1;
        if (int.TryParse(output, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
            Output = output;
        }
    }
}