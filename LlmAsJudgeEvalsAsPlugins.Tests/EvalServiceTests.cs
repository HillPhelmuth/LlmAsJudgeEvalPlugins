using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Xunit;
using Moq;

namespace LlmAsJudgeEvalsAsPlugins.Tests;
//[Collection(nameof(EvalServiceFixture))]
public class EvalServiceTests(EvalServiceFixture fixture) : IClassFixture<EvalServiceFixture>
{
    private const string TestYaml = """
                                    name: binary_quality
                                    description: Return 4 if the answer is perfect, 0 otherwise.
                                    template: |
                                      Give me just "4".
                                    """;

    private sealed record TestInput(string SystemAnswer)
        : IInputModel
    {
        public string FunctionName => "binary_quality";
        public KernelArguments RequiredInputs =>
            new()
            {
                ["answer"] = SystemAnswer
            };
    }

    [Fact]
    public void AddEvalFunction_AddsFunction_WhenNotExists()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var service = new EvalService(kernel);
        var function = KernelFunctionFactory.CreateFromPrompt("prompt", new OpenAIPromptExecutionSettings(), "testFunction");
        var name = "testFunction";

        // Act
        service.AddEvalFunction(name, function);

        // Assert
        var evalFunctions = typeof(EvalService).GetProperty("EvalFunctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(service) as Dictionary<string, KernelFunction>;
        Assert.True(evalFunctions!.ContainsKey(name));
        Assert.Equal(function, evalFunctions[name]);
    }

    [Fact]
    public void AddEvalFunction_Override_ReplacesFunction()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var service = new EvalService(kernel);
        var function1 = KernelFunctionFactory.CreateFromPrompt("prompt1", new OpenAIPromptExecutionSettings(), "testFunction");
        var function2 = KernelFunctionFactory.CreateFromPrompt("prompt2", new OpenAIPromptExecutionSettings(), "testFunction");
        var name = "testFunction";
        service.AddEvalFunction(name, function1);

        // Act
        service.AddEvalFunction(name, function2, overrideExisting: true);

        // Assert
        var evalFunctions = typeof(EvalService).GetProperty("EvalFunctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(service) as Dictionary<string, KernelFunction>;
        Assert.Equal(function2, evalFunctions![name]);
    }

    [Fact]
    public void AddEvalFunction_FromPrompt_AddsFunction()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var service = new EvalService(kernel);
        var prompt = "Test prompt";
        var settings = new OpenAIPromptExecutionSettings();
        var name = "promptFunction";

        // Act
        fixture.Sut.AddEvalFunction(name, prompt, settings);

        // Assert
        var evalFunctions = typeof(EvalService).GetProperty("EvalFunctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(fixture.Sut) as Dictionary<string, KernelFunction>;
        Assert.True(evalFunctions!.ContainsKey(name));
    }

    [Fact]
    public void AddEvalFunction_FromYaml_AddsFunction()
    {
        //Act
        fixture.Sut.AddEvalFunctionFromYaml(TestYaml, "binary_quality", true);

        //Assert
        var evalFunctions = typeof(EvalService).GetProperty("EvalFunctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(fixture.Sut) as Dictionary<string, KernelFunction>;
        Assert.True(evalFunctions!.ContainsKey("binary_quality"));
    }

    [Fact]
    public void AggregateResults_ReturnsAverageScore()
    {
        // Arrange
        var scores = new List<ResultScore>
        {
            new ResultScore("Eval1", "2"),
            new ResultScore("Eval1", "4"),
            new ResultScore("Eval2", "3")
        };
        scores[0].ProbScore = 0.5;
        scores[1].ProbScore = 0.7;
        scores[2].ProbScore = 0.2;

        // Act
        var result = EvalService.AggregateResults(scores);
        var resultProb = EvalService.AggregateResults(scores, useLogProbs: true);

        // Assert
        Assert.Equal(3.0, result["Eval1"]);
        Assert.Equal(3.0, result["Eval2"]);
        Assert.Equal(0.6, resultProb["Eval1"], 1);
        Assert.Equal(0.2, resultProb["Eval2"]);
    }

    [Fact]
    public void AddEvalFunctionFromYaml_AddsFunction()
    {
        // Arrange
        var function = KernelFunctionFactory.CreateFromPrompt("prompt", new OpenAIPromptExecutionSettings(), "yamlFunction");
        var kernel = Kernel.CreateBuilder().Build();
        Func<string, KernelFunction> factory = yaml => function;
        var service = new EvalService(kernel, factory);
        var yaml = TestYaml;
        var name = "binary_quality";
        // Act
        service.AddEvalFunctionFromYaml(yaml, name);
        // Assert
        var evalFunctions = typeof(EvalService).GetProperty("EvalFunctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(service) as Dictionary<string, KernelFunction>;
        Assert.True(evalFunctions!.ContainsKey(name));
    }

    [Fact]
    public void AddEvalFunctionFromYaml_Stream_AddsFunction()
    {
        // Arrange
        var service = fixture.Sut;
        var yaml = TestYaml;
        var name = "binary_quality";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));
        // Act
        service.AddEvalFunctionFromYaml(stream, name);
        // Assert
        var evalFunctions = typeof(EvalService).GetProperty("EvalFunctions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(service) as Dictionary<string, KernelFunction>;
        Assert.True(evalFunctions!.ContainsKey(name));
    }

    [Fact]
    public async Task ExecuteEval_ReturnsExpectedScore()
    {
        // Arrange

        fixture.Sut.AddEvalFunctionFromYaml(TestYaml, "binary_quality", true);
        // Act
        var score = await fixture.Sut.ExecuteEval(new TestInput("any"));

        // Assert
        Assert.Equal(4, score.Score);          // parsed from assistant reply
        Assert.Equal("binary_quality", score.EvalName);
    }
}