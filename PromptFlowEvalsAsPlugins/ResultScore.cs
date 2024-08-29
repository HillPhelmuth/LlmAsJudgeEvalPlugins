using Microsoft.SemanticKernel;

namespace PromptFlowEvalsAsPlugins;

public class ResultScore
{
	public string EvalName { get; set; }


	public int Score { get; set; } = -1;
	public double ProbScore { get; set; }

	/// <summary>
	/// Actual response if it could not be converted into a score
	/// </summary>
	public string? Output { get; set; }

	public string? Reasoning { get; set; }
	public string? ReferenceAnswer { get; set; }
	public List<TokenString>? LogProbResults { get; set; }
	/// <summary>
	/// Should be a score from 1-5; -1 Represents an unparsable result
	/// </summary>
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
	public ResultScore(string name, ScorePlusResponse? scorePlusResponse, IEnumerable<TokenString> tokenStrings)
	{
		EvalName = name;
		var logProb = LogProbAnalyzer.GetTokenAfterScore(tokenStrings);
		var logProbVals = logProb.TopLogProbs;
		var output = logProb?.StringValue;
		LogProbResults = logProbVals;
		EvalName = name;
		ProbScore = logProbVals.Select(x => x.AsTokenProb()).NormalizeValues().CalculateWeightedScore();
		if (int.TryParse(output, out var parsedScore))
		{
			Score = parsedScore;
		}
		else
		{
			Output = output;
		}
		Reasoning = scorePlusResponse?.QualityScoreReasoning;
		
	}
}

public class LogProbAnalyzer
{
	public static TokenString? GetTokenAfterScore(IEnumerable<TokenString> tokens)
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