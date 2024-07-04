using Azure.AI.OpenAI;

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
			var tokenString = new TokenString(logProb.Token, logProb.ToLinearProb());
			if (logProb.TopLogProbabilityEntries is { Count: > 0 })
			{
				var innerResult = logProb.TopLogProbabilityEntries.Select(item => new TokenString(item.Token, item.ToLinearProb())).ToList();
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

		return weightedSum;
	}
	public static IEnumerable<TokenProb> NormalizeValues(this IEnumerable<TokenProb> tokenProbs)
	{
		
		double sum = tokenProbs.Sum(x => x.Probability);

		if (sum == 0)
		{
			throw new ArgumentException("The sum of the values in the TokenProb is zero.");
		}
		foreach (var token in tokenProbs)
		{
			var normalizeValues = new TokenProb(token.StringValue, token.Probability / sum);
			Console.WriteLine($"Normalized to {normalizeValues} (from {token})");
			yield return normalizeValues;
		}
		//return tokenProbs.Select(token => new TokenProb(token.StringValue, token.Probability / sum));
	}
	public static double ToLinearProb(this ChatTokenLogProbabilityResult logProbabilityResult) => Math.Exp(logProbabilityResult.LogProbability);

	public static double ToLinearProb(this ChatTokenLogProbabilityInfo logProbInfo) => Math.Exp(logProbInfo.LogProbability);
}
