using HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

namespace LlmAsJudgeEvalsAsPlugins.Demo.Models.Helpers;

public static class Extensions
{
    public static IInputModel? AsInputModel(this EvalType evalType, Dictionary<string, string> args)
    {
        if (!EvaluateArgsDictionary(evalType, args))
        {
            return null;
        }
        IInputModel? inputModel = evalType switch
        {
            EvalType.GptGroundedness => InputModel.GroundednessModel(
                args["answer"], args["question"], args["context"]),
            EvalType.GptGroundedness2 => InputModel.Groundedness2Model(
                args["answer"], args["question"], args["context"]),
            EvalType.GptSimilarity => InputModel.SimilarityModel(
                args["answer"], args["ground_truth"], args["question"]),
            EvalType.Relevance => InputModel.RelevanceModel(
                args["answer"], args["question"], args["context"]),
            EvalType.Coherence => InputModel.CoherenceModel(
                args["answer"], args["question"]),
            EvalType.PerceivedIntelligence => InputModel.PerceivedIntelligenceModel(
                args["answer"], args["question"], args["context"]),
            EvalType.PerceivedIntelligenceNonRag => InputModel.PerceivedIntelligenceNonRagModel(
                args["answer"], args["question"]),
            EvalType.Fluency => InputModel.FluencyModel(
                args["answer"], args["question"]),
            EvalType.Empathy => InputModel.EmpathyModel(
                args["answer"], args["question"]),
            EvalType.Helpfulness => InputModel.HelpfulnessModel(
                args["answer"], args["question"]),
            EvalType.Retrieval => InputModel.RetrievalModel(
                args["question"], args["context"]),
            EvalType.GptGroundednessExplain => InputModel.GroundednessExplainModel(
                args["answer"], args["question"], args["context"]),
            EvalType.GptGroundedness2Explain => InputModel.Groundedness2ExplainModel(
                args["answer"], args["question"], args["context"]),
            EvalType.GptSimilarityExplain => InputModel.SimilarityExplainModel(
                args["answer"], args["ground_truth"], args["question"]),
            EvalType.RelevanceExplain => InputModel.RelevanceExplainModel(
                args["answer"], args["question"], args["context"]),
            EvalType.CoherenceExplain => InputModel.CoherenceExplainModel(
                args["answer"], args["question"]),
            EvalType.PerceivedIntelligenceExplain => InputModel.PerceivedIntelligenceExplainModel(
                args["answer"], args["question"], args["context"]),
            EvalType.PerceivedIntelligenceNonRagExplain => InputModel.PerceivedIntelligenceNonRagExplainModel(
                args["answer"], args["question"]),
            EvalType.FluencyExplain => InputModel.FluencyExplainModel(
                args["answer"], args["question"]),
            EvalType.EmpathyExplain => InputModel.EmpathyExplainModel(
                args["answer"], args["question"]),
            EvalType.HelpfulnessExplain => InputModel.HelfulnessExplainModel(
                args["answer"], args["question"]),
            EvalType.RetrievalExplain => InputModel.RetrievalExplainModel(
                args["question"], args["context"]),
            EvalType.RoleAdherence => InputModel.RoleAdherenceModel(
                args["answer"], args["question"], args.GetValueOrDefault("instructions", "none")),
            EvalType.ExcessiveAgency => InputModel.ExcessiveAgencyModel(
                args["answer"], args["question"], args.GetValueOrDefault("instructions", "none")),
            EvalType.ExcessiveAgencyExplain => InputModel.ExcessiveAgencyExplainModel(
                args["answer"], args["question"], args.GetValueOrDefault("instructions", "none")),
            EvalType.RoleAdherenceExplain => InputModel.RoleAdherenceExplainModel(
                args["answer"], args["question"], args.GetValueOrDefault("instructions", "none")),

            _ => null
        };
        return inputModel;
    }
    private static bool EvaluateArgsDictionary(EvalType evalType, Dictionary<string, string> args)
    {
        return evalType switch
        {
            // Basic eval types
            EvalType.GptGroundedness => args.ContainsKey("answer") && args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.GptGroundedness2 => args.ContainsKey("answer") && args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.GptSimilarity => args.ContainsKey("answer") && args.ContainsKey("ground_truth") && args.ContainsKey("question"),
            EvalType.Relevance => args.ContainsKey("answer") && args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.Coherence => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.PerceivedIntelligence => args.ContainsKey("answer") && args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.PerceivedIntelligenceNonRag => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.Fluency => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.Empathy => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.Helpfulness => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.Retrieval => args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.RoleAdherence => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.ExcessiveAgency => args.ContainsKey("answer") && args.ContainsKey("question"),
            // Explain variants (same requirements as their base types)
            EvalType.GptGroundednessExplain => args.ContainsKey("answer") && args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.GptGroundedness2Explain => args.ContainsKey("answer") && args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.GptSimilarityExplain => args.ContainsKey("answer") && args.ContainsKey("ground_truth") && args.ContainsKey("question"),
            EvalType.RelevanceExplain => args.ContainsKey("answer") && args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.CoherenceExplain => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.PerceivedIntelligenceExplain => args.ContainsKey("answer") && args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.PerceivedIntelligenceNonRagExplain => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.FluencyExplain => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.EmpathyExplain => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.HelpfulnessExplain => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.RetrievalExplain => args.ContainsKey("question") && args.ContainsKey("context"),
            EvalType.ExcessiveAgencyExplain => args.ContainsKey("answer") && args.ContainsKey("question"),
            EvalType.RoleAdherenceExplain => args.ContainsKey("answer") && args.ContainsKey("question"),

            _ => false
        };
    }
}