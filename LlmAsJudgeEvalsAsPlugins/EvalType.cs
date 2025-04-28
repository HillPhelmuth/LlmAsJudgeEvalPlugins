namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

/// <summary>
/// Represents the evaluation type.
/// </summary>
public enum EvalType
{
    /// <summary>
    /// Evaluates factual accuracy and support in context (1-5 scale).
    /// </summary>
    GptGroundedness,

    /// <summary>
    /// Alternative groundedness evaluation with finer granularity (1-10 scale).
    /// </summary>
    GptGroundedness2,

    /// <summary>
    /// Measures response similarity to reference text.
    /// </summary>
    GptSimilarity,

    /// <summary>
    /// Assesses response relevance to prompt/question.
    /// </summary>
    Relevance,

    /// <summary>
    /// Evaluates logical flow and consistency.
    /// </summary>
    Coherence,

    /// <summary>
    /// Rates apparent knowledge and reasoning with RAG (Retrieval Augmented Generation).
    /// </summary>
    PerceivedIntelligence,

    /// <summary>
    /// Rates apparent knowledge and reasoning without RAG.
    /// </summary>
    PerceivedIntelligenceNonRag,

    /// <summary>
    /// Measures natural language quality.
    /// </summary>
    Fluency,

    /// <summary>
    /// Assesses emotional understanding.
    /// </summary>
    Empathy,

    /// <summary>
    /// Evaluates practical value of response.
    /// </summary>
    Helpfulness,

    /// <summary>
    /// Evaluates the retrieved content based on the query.
    /// </summary>
    Retrieval,

    /// <summary>
    /// Evaluates the degree to which the response exhibits excessive agency or autonomy.
    /// </summary>
    ExcessiveAgency,

    /// <summary>
    /// Assesses adherence to the expected role or persona in the response.
    /// </summary>
    RoleAdherence,

    /// <summary>
    /// Evaluates factual accuracy and support in context (1-5 scale) with detailed explanation.
    /// </summary>
    GptGroundednessExplain,

    /// <summary>
    /// Alternative groundedness evaluation (1-10 scale) with detailed explanation.
    /// </summary>
    GptGroundedness2Explain,

    /// <summary>
    /// Measures response similarity to reference text with detailed explanation.
    /// </summary>
    GptSimilarityExplain,

    /// <summary>
    /// Assesses response relevance to prompt/question with detailed explanation.
    /// </summary>
    RelevanceExplain,

    /// <summary>
    /// Evaluates logical flow and consistency with detailed explanation.
    /// </summary>
    CoherenceExplain,

    /// <summary>
    /// Rates apparent knowledge and reasoning with RAG, including detailed explanation.
    /// </summary>
    PerceivedIntelligenceExplain,

    /// <summary>
    /// Rates apparent knowledge and reasoning without RAG, including detailed explanation.
    /// </summary>
    PerceivedIntelligenceNonRagExplain,

    /// <summary>
    /// Measures natural language quality with detailed explanation.
    /// </summary>
    FluencyExplain,

    /// <summary>
    /// Assesses emotional understanding with detailed explanation.
    /// </summary>
    EmpathyExplain,

    /// <summary>
    /// Evaluates practical value of response with detailed explanation.
    /// </summary>
    HelpfulnessExplain,

    /// <summary>
    /// Evaluates the retrieved content based on the query with detailed explanation.
    /// </summary>
    RetrievalExplain,

    /// <summary>
    /// Evaluates the degree to which the response exhibits excessive agency or autonomy, with detailed explanation.
    /// </summary>
    ExcessiveAgencyExplain,

    /// <summary>
    /// Assesses adherence to the expected role or persona in the response, with detailed explanation.
    /// </summary>
    RoleAdherenceExplain
}
