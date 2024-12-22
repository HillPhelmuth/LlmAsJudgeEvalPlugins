using Microsoft.SemanticKernel;

namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

/// <summary>
/// Represents an input model for evaluation.
/// </summary>
public class InputModel : IInputModel
{
	private InputModel(EvalType evalType, KernelArguments kernelArgs)
	{
		EvalType = evalType;
		RequiredInputs = kernelArgs;
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="InputModel"/> class using one the pre-made evals.
	/// </summary>
	/// <param name="evalType">The evaluation type.</param>
	/// <param name="kernelArgs">The kernel arguments.</param>
	public static InputModel CreateCoreEvalInputModel(EvalType evalType, KernelArguments kernelArgs)
	{
		return new InputModel(evalType, kernelArgs);
	}

	private EvalType EvalType { get; }

	/// <summary>
	/// Gets the Semantic Kernel function name.
	/// </summary>
	public virtual string FunctionName => Enum.GetName(EvalType)!;

	/// <summary>
	/// Gets the required KernelArguments inputs.
	/// </summary>
	public KernelArguments RequiredInputs { get; }

	/// <summary>
	/// Creates an input model for groundedness evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <param name="context">The context/RAG content.</param>
	/// <returns>The input model for groundedness evaluation.</returns>
	public static InputModel GroundednessModel(string answer, string question, string context) => CreateCoreEvalInputModel(EvalType.GptGroundedness, new KernelArguments
	{
		["answer"] = answer,
		["context"] = context,
		["question"] = question
	});

	/// <summary>
	/// Creates an input model for similarity evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="groundTruth">The ground truth, correct, ideal, or preferred response.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for similarity evaluation.</returns>
	public static InputModel SimilarityModel(string answer, string groundTruth, string question) => CreateCoreEvalInputModel(EvalType.GptSimilarity, new KernelArguments
	{
		["answer"] = answer,
		["ground_truth"] = groundTruth,
		["question"] = question
	});

	/// <summary>
	/// Creates an input model for relevance evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="context">The context/RAG content</param>
	/// <returns>The input model for relevance evaluation.</returns>
	public static InputModel RelevanceModel(string answer, string question, string context) => CreateCoreEvalInputModel(EvalType.Relevance, new KernelArguments
	{
		["answer"] = answer,
		["context"] = context,
		["question"] = question
	});

	/// <summary>
	/// Creates an input model for coherence evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for coherence evaluation.</returns>
	public static InputModel CoherenceModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.Coherence, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question
	});

	/// <summary>
	/// Creates an input model for groundedness2 evaluation. Scores 1-10
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <param name="context">The context/RAG content</param>
	/// <returns>The input model for groundedness2 evaluation.</returns>
	public static InputModel Groundedness2Model(string answer, string question, string context) => CreateCoreEvalInputModel(EvalType.GptGroundedness2, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
		["context"] = context
	});

	/// <summary>
	/// Creates an input model for perceived intelligence evaluation. Scores 1-10
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <param name="context">The context/RAG content</param>
	/// <returns>The input model for perceived intelligence evaluation.</returns>
	public static InputModel PerceivedIntelligenceModel(string answer, string question, string context) => CreateCoreEvalInputModel(EvalType.PerceivedIntelligence, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
		["context"] = context
	});
	/// <summary>
	/// Creates an input model for perceived intelligence evaluation without RAG (Response-Answer-Grade) content. Scores 1-10
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for perceived intelligence evaluation without RAG content.</returns>
	public static InputModel PerceivedIntelligenceNonRagModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.PerceivedIntelligenceNonRag, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
	});
	/// <summary>
	/// Creates an input model for fluency evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for fluency evaluation.</returns>
	public static InputModel FluencyModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.Fluency, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
	});
	/// <summary>
	/// Creates an input model for empathy evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for empathy evaluation.</returns>
	public static InputModel EmpathyModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.Empathy, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
	});
	/// <summary>
	/// Creates an input model for helpfulness evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for helpfulness evaluation.</returns>
	public static InputModel HelpfulnessModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.Helpfulness, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
	});

	/// <summary>
	/// Creates an input model for groundedness explain evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <param name="context">The context/RAG content.</param>
	/// <returns>The input model for groundedness explain evaluation.</returns>
	public static InputModel GroundednessExplainModel(string answer, string question, string context) => CreateCoreEvalInputModel(EvalType.GptGroundednessExplain, new KernelArguments
	{
		["answer"] = answer,
		["context"] = context,
		["question"] = question
	});

	/// <summary>
	/// Creates an input model for groundedness2 explain evaluation. Scores 1-10
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <param name="context">The context/RAG content</param>
	/// <returns>The input model for groundedness2 explain evaluation.</returns>
	public static InputModel Groundedness2ExplainModel(string answer, string question, string context) => CreateCoreEvalInputModel(EvalType.GptGroundedness2Explain, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
		["context"] = context
	});

	/// <summary>
	/// Creates an input model for similarity explain evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="groundTruth">The ground truth, correct, ideal, or preferred response.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for similarity explain evaluation.</returns>
	public static InputModel SimilarityExplainModel(string answer, string groundTruth, string question) => CreateCoreEvalInputModel(EvalType.GptSimilarityExplain, new KernelArguments
	{
		["answer"] = answer,
		["ground_truth"] = groundTruth,
		["question"] = question
	});

	/// <summary>
	/// Creates an input model for relevance explain evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="context">The context/RAG content</param>
	/// <returns>The input model for relevance explain evaluation.</returns>
	public static InputModel RelevanceExplainModel(string answer, string question, string context) => CreateCoreEvalInputModel(EvalType.RelevanceExplain, new KernelArguments
	{
		["answer"] = answer,
		["context"] = context,
		["question"] = question
	});

	/// <summary>
	/// Creates an input model for coherence explain evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for coherence explain evaluation.</returns>
	public static InputModel CoherenceExplainModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.CoherenceExplain, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question
	});

	/// <summary>
	/// Creates an input model for perceived intelligence explain evaluation. Scores 1-10
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <param name="context">The context/RAG content</param>
	/// <returns>The input model for perceived intelligence explain evaluation.</returns>
	public static InputModel PerceivedIntelligenceExplainModel(string answer, string question, string context) => CreateCoreEvalInputModel(EvalType.PerceivedIntelligenceExplain, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
		["context"] = context
	});

	/// <summary>
	/// Creates an input model for perceived intelligence explain evaluation without RAG (Response-Answer-Grade) content. Scores 1-10
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for perceived intelligence explain evaluation without RAG content.</returns>
	public static InputModel PerceivedIntelligenceNonRagExplainModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.PerceivedIntelligenceNonRagExplain, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
	});

	/// <summary>
	/// Creates an input model for fluency explain evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for fluency explain evaluation.</returns>
	public static InputModel FluencyExplainModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.FluencyExplain, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
	});

	/// <summary>
	/// Creates an input model for empathy explain evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for empathy explain evaluation.</returns>
	public static InputModel EmpathyExplainModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.EmpathyExplain, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
	});

	/// <summary>
	/// Creates an input model for helpfulness explain evaluation. Scores 1-5
	/// </summary>
	/// <param name="answer">The response being evaluated.</param>
	/// <param name="question">The question.</param>
	/// <returns>The input model for helpfulness explain evaluation.</returns>
	public static InputModel HelfulnessExplainModel(string answer, string question) => CreateCoreEvalInputModel(EvalType.HelpfulnessExplain, new KernelArguments
	{
		["answer"] = answer,
		["question"] = question,
	});
	
}
