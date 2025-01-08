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
	internal static List<TokenString> AsTokenStrings(this IReadOnlyList<ChatTokenLogProbabilityDetails> logProbContentItems)
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
        var parsableTokens = tokenProbs
            .Where(token => int.TryParse(token.StringValue, out _))
            .ToList();

        // Calculate the total probability of the parsable values
        double totalProbability = parsableTokens.Sum(token => token.Probability);

        // Adjust probabilities to sum to 100%
        var adjustedTokens = parsableTokens
            .Select(token => new TokenProb(token.StringValue, token.Probability / totalProbability)).ToList();
        tokenProbs.ToList().ForEach(tp => Console.WriteLine(tp.ToString()));
		
		if (!adjustedTokens.Any())
		{
			throw new InvalidOperationException("No valid tokens to calculate the weighted average.");
		}

		var weightedSum = adjustedTokens.Sum(vt => int.Parse(vt.StringValue) * vt.Probability);

		return weightedSum;
	}

    private static double ToLinearProb(this ChatTokenTopLogProbabilityDetails logProbabilityResult) => Math.Exp(logProbabilityResult.LogProbability);

	private static double ToLinearProb(this ChatTokenLogProbabilityDetails logProbInfo) => Math.Exp(logProbInfo.LogProbability);
}
