using HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using Xunit;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace LlmAsJudgeEvalsAsPlugins.Tests;

// TestFixture.cs – one per test class
public sealed class EvalServiceFixture : IAsyncLifetime
{
    public Kernel Kernel { get; private set; } = default!;
    public IChatCompletionService FakeChat { get; } = CreateFakeChat();

    public EvalService Sut { get; private set; } = default!;     // system-under-test

    public Task InitializeAsync()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(FakeChat);   // ≤— inject the fake LLM
        Kernel = builder.Build();
        Sut = new EvalService(Kernel);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // Creates a Moq that pretends to be the LLM.
    private static IChatCompletionService CreateFakeChat()
    {
        var mock = new Mock<IChatCompletionService>();

        // Fake assistant reply that includes log-probs in plain JSON-friendly form
        var fakeAssistantReply = new ChatMessageContent(
            AuthorRole.Assistant,
            "4",                                             // the model’s text
            metadata: new Dictionary<string, object?>
            {
                ["ContentTokenLogProbabilities"] = new[]
                {
                    new
                    {
                        Tokens          = new[] { "1","2","3","4","5" },
                        LogProbabilities = new[] { -2.1, -1.9, -1.7, -0.01, -0.05 }
                    }
                }
            });

        mock.Setup(x => x.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([fakeAssistantReply]);

        return mock.Object;
    }

}
