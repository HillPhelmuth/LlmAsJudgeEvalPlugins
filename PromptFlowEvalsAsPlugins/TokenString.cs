using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PromptFlowEvalsAsPlugins;

public record TokenString
{
	public TokenString(string StringValue, double LogProb, int Token = 0)
	{
		this.Token = Token;
		this.LogProb = LogProb;
		this.StringValue = StringValue;
		NormalizedLogProbability = LogProb;
	}

	public string StringValue { get; set; }
	public List<TokenString> TopLogProbs { get; set; } = [];
	public double NormalizedLogProbability { get; set; }
	public int Token { get; init; }
	public double LogProb { get; init; }
	public TokenProb AsTokenProb()
	{
		return new TokenProb(StringValue, NormalizedLogProbability);
	}

}
public class TokenProb(string stringValue, double normLogProb)
{
	public string StringValue { get; set; } = stringValue;
	public double Probability { get; set; } = normLogProb;

    public override string ToString()
    {
        return $"Token: {StringValue}, Probability: {Probability}";
    }
}
public static class LogProbExts
{
	public static List<TokenString> AsTokenStrings(this IEnumerable<ChatTokenLogProbabilityResult> logProbContentItems)
	{
		var result = new List<TokenString>();
		foreach (var logProb in logProbContentItems)
		{
			var tokenString = new TokenString(logProb.Token, logProb.NormalizedLogProb());
			if (logProb.TopLogProbabilityEntries is { Count: > 0 })
			{
				var innerResult = logProb.TopLogProbabilityEntries.Select(item => new TokenString(item.Token, item.NormalizedLogProb())).ToList();
				tokenString.TopLogProbs = innerResult;
			}
			result.Add(tokenString);
		}
		return result;
	}
	public static double CalculateWeightedScore(this IEnumerable<TokenProb> tokenProbs)
	{
		tokenProbs.ToList().ForEach(tp => Console.WriteLine(tp.ToString()));
		var validTokens = tokenProbs
			.Where(tp => int.TryParse(tp.StringValue, out _))
			.Select(tp => new
			{
				Value = int.Parse(tp.StringValue),
				tp.Probability
			}).ToList();

		if (!validTokens.Any())
		{
			throw new InvalidOperationException("No valid tokens to calculate the weighted average.");
		}

		var weightedSum = validTokens.Sum(vt => vt.Value * vt.Probability);

		return weightedSum /*/ validTokens.Count*/;
	}
	public static double NormalizedLogProb(this ChatTokenLogProbabilityResult logProbabilityResult)
	{
		return Math.Exp(logProbabilityResult.LogProbability);
	}
	public static double NormalizedLogProb(this ChatTokenLogProbabilityInfo logProbInfo)
	{
		return Math.Exp(logProbInfo.LogProbability);
	}
}
