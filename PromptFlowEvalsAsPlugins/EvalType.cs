namespace PromptFlowEvalsAsPlugins;

/// <summary>
/// Represents the evaluation type.
/// </summary>
public enum EvalType
{
    GptGroundedness,
    GptGroundedness2,
    GptSimilarity,
    Relevance,
    Coherence,
    PerceivedIntelligence,
    PerceivedIntelligenceNonRag,
    Fluency,
    Empathy,
    Helpfulness
}