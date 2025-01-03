using Microsoft.SemanticKernel;

namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

/// <summary>
/// Represents the result score of an evaluation.
/// </summary>
public class ResultScore
{
    /// <summary>
    /// Gets or sets the name of the evaluation.
    /// </summary>
    public string EvalName { get; set; }

    /// <summary>
    /// Gets or sets the score of the evaluation.
    /// </summary>
    public int Score { get; set; } = -1;

    /// <summary>
    /// Gets or sets the probability weighted score of the evaluation.
    /// </summary>
    public double ProbScore { get; set; }

    /// <summary>
    /// Gets or sets the actual response if it could not be converted into a score.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Gets or sets the reasoning behind the quality score.
    /// </summary>
    public string? Reasoning { get; set; }
    /// <summary>
    /// Gets or sets the chain of thought for the evaluation.
    /// </summary>
    public string? ChainOfThought { get; set; }

    /// <summary>
    /// Gets or sets the reference answer for the evaluation.
    /// </summary>
    public string? ReferenceAnswer { get; set; }

    /// <summary>
    /// Gets or sets the log probability results of the evaluation.
    /// </summary>
    public List<TokenString>? LogProbResults { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultScore"/> class with a function result.
    /// </summary>
    /// <param name="name">The name of the evaluation.</param>
    /// <param name="result">The function result.</param>
    public ResultScore(string name, FunctionResult result)
    {
        EvalName = name;
        var output = result.GetValue<string>()?.Replace("Score:", "").Trim();
        if (int.TryParse(output, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
            Output = output;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultScore"/> class with a string result.
    /// </summary>
    /// <param name="name">The name of the evaluation.</param>
    /// <param name="result">The string result.</param>
    public ResultScore(string name, string result)
    {
        EvalName = name;
        var output = result.Replace("Score:", "").Trim();
        if (int.TryParse(output, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
            Output = output;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultScore"/> class with a log probability.
    /// </summary>
    /// <param name="name">The name of the evaluation.</param>
    /// <param name="logProb">The log probability.</param>
    public ResultScore(string name, TokenString logProb)
    {
        Console.WriteLine($"Topline: {name} - {logProb.StringValue}");
        var logProbVals = logProb.TopLogProbs;
        LogProbResults = logProbVals;
        EvalName = name;
        ProbScore = logProbVals.Select(x => x.AsTokenProb()).NormalizeValues().CalculateWeightedScore();
        if (int.TryParse(logProb.StringValue, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
            Output = logProb.StringValue;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultScore"/> class with a score plus response and token strings.
    /// </summary>
    /// <param name="name">The name of the evaluation.</param>
    /// <param name="scorePlusResponse">The score plus response.</param>
    /// <param name="tokenStrings">The token strings.</param>
    public ResultScore(string name, ScorePlusResponse? scorePlusResponse, IEnumerable<TokenString> tokenStrings)
    {
        EvalName = name;
        Console.WriteLine(string.Join("", tokenStrings.Select(x => x.StringValue)));
        var logProb = GetTokenAfterScore(tokenStrings);
        var logProbVals = logProb?.TopLogProbs;
        var output = logProb?.StringValue;
        LogProbResults = logProbVals;
        EvalName = name;
        ProbScore = logProbVals?.Select(x => x.AsTokenProb()).NormalizeValues().CalculateWeightedScore() ?? -1;
        if (int.TryParse(output, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
            Output = output;
        }
        Reasoning = scorePlusResponse?.QualityScoreReasoning;
        ChainOfThought = scorePlusResponse?.ChainOfThought;
    }
    private static TokenString? GetTokenAfterScore(IEnumerable<TokenString> tokens)
    {
        var scoreString = "\"score\": ";
        var currentString = string.Empty;
        var tokenList = tokens.ToList();

        for (var i = 0; i < tokenList.Count; i++)
        {
            currentString += tokenList[i].StringValue;

            // Check if the current string contains the target sequence
            if (currentString.Contains(scoreString))
            {
                // Calculate the position immediately following the scoreString
                var scoreEndIndex = currentString.IndexOf(scoreString, StringComparison.Ordinal) + scoreString.Length;

                // Find the token that follows the scoreString
                var remainingString = currentString[scoreEndIndex..];
                if (!string.IsNullOrEmpty(remainingString))
                {
                    var remainingTokenIndex = i - (remainingString.Length - 1);
                    return tokenList[remainingTokenIndex];
                }
                else if (i + 1 < tokenList.Count)
                {
                    return tokenList[i + 1];
                }
            }
        }

        return null;
    }
}