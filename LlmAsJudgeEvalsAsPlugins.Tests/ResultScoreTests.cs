using HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;
using Xunit;

namespace LlmAsJudgeEvalsAsPlugins.Tests;

public class ResultScoreTests
{
    [Fact]
    public void Ctor_ScorePlusResponse_DoesNotUseTokenStrings_WhenNotProvided()
    {
        // Arrange
        const string name = "eval";
        const int qualityScore = 4;

        var scorePlusResponse = new ScorePlusResponse
        {
            QualityScore = qualityScore,
            QualityScoreReasoning = "reason",
            ChainOfThought = "cot"
        };

        // Act
        var result = new ResultScore(name, scorePlusResponse, tokenStrings: null);

        // Assert
        Assert.Equal(name, result.EvalName);
        Assert.Equal(qualityScore, result.Score);
        Assert.Equal(qualityScore, result.ProbScore);
        Assert.Equal("reason", result.Reasoning);
        Assert.Equal("cot", result.ChainOfThought);

        Assert.Null(result.LogProbResults);
        Assert.Null(result.Output);
    }
}
