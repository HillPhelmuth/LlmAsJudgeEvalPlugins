using OpenAI.Chat;

namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

public record TokenString
{
	internal TokenString(string StringValue, double LogProb, int Token = 0)
	{
		this.Token = Token;
		this.LogProb = LogProb;
		this.StringValue = StringValue;
		NormalizedLogProbability = LogProb;
	}

	public string StringValue { get; }
	public List<TokenString> TopLogProbs { get; set; } = [];
	public double NormalizedLogProbability { get; }
	public int Token { get; init; }
	public double LogProb { get; init; }
	internal TokenProb AsTokenProb()
	{
		return new TokenProb(StringValue, NormalizedLogProbability);
	}

}
internal class TokenProb(string stringValue, double normLogProb)
{
	public string StringValue { get; set; } = stringValue;
	public double Probability { get; set; } = normLogProb;

    public override string ToString()
    {
        return $"Token: {StringValue}, Probability: {Probability}";
    }
}
internal static class LogProbExts
{
	internal static List<TokenString> AsTokenStrings(this IReadOnlyList<ChatTokenLogProbabilityInfo> logProbContentItems)
	{
		var result = new List<TokenString>();
		foreach (var logProb in logProbContentItems)
		{
			var tokenString = new TokenString(logProb.Token, logProb.ToLinearProb());
			if (logProb.TopLogProbabilities is { Count: > 0 })
			{
				var innerResult = logProb.TopLogProbabilities.Select(item => new TokenString(item.Token, item.ToLinearProb())).ToList();
				tokenString.TopLogProbs = innerResult;
			}
			result.Add(tokenString);
		}
		return result;
	}
	internal static double CalculateWeightedScore(this IEnumerable<TokenProb> tokenProbs)
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
	internal static IEnumerable<TokenProb> NormalizeValues(this IEnumerable<TokenProb> tokenProbs)
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
	private static double ToLinearProb(this ChatTokenTopLogProbabilityInfo logProbabilityResult) => Math.Exp(logProbabilityResult.LogProbability);

	private static double ToLinearProb(this ChatTokenLogProbabilityInfo logProbInfo) => Math.Exp(logProbInfo.LogProbability);
}
