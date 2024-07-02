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
		EvalName = name;
		ProbScore = logProbVals.Select(x => x.AsTokenProb()).CalculateWeightedScore();
		if (int.TryParse(logProb.StringValue, out var parsedScore))
        {
            Score = parsedScore;
        }
        else
        {
			Output = logProb.StringValue;
        }
		//Score = (int) ProbScore;

	}
	public ResultScore(string name, ScorePlusResponse? scorePlusResponse)
	{
		EvalName = name;
		var output = scorePlusResponse?.QualityScore?.Replace("Score:", "").Trim();
		if (int.TryParse(output, out var parsedScore))
		{
			Score = parsedScore;
		}
		else
		{
			Output = output;
		}
		Reasoning = scorePlusResponse?.QualityScoreReasoning;
		ReferenceAnswer = scorePlusResponse?.ReferenceAnswer;
	}
}