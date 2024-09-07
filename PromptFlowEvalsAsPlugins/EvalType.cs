namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

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
    Helpfulness,
    GptGroundednessExplain,
    GptGroundedness2Explain,
    GptSimilarityExplain,
    RelevanceExplain,
    CoherenceExplain,
    PerceivedIntelligenceExplain,
    PerceivedIntelligenceNonRagExplain,
    FluencyExplain,
    EmpathyExplain,
    HelpfulnessExplain
    
}