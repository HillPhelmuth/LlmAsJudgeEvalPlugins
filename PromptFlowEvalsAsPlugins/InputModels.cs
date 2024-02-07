using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PromptFlowEvalsAsPlugins
{
    public class InputModel
    {
        private InputModel(EvalType evalType, KernelArguments kernelArgs)
        {
            EvalType = evalType;
            RequiredInputs = kernelArgs;
        }

        private EvalType EvalType { get; }
        public string FunctionName => Enum.GetName(EvalType)!;
        public KernelArguments RequiredInputs { get; }
        public static InputModel GroundednessModel(string answer, string context) => new(EvalType.GptGroundedness, new KernelArguments
        {
            ["answer"] = answer,
            ["context"] = context
        });
        public static InputModel SimilarityModel(string answer, string groundTruth, string question) => new(EvalType.GptSimilarity, new KernelArguments
        {
            ["answer"] = answer,
            ["ground_truth"] = groundTruth,
            ["question"] = question
        });
        public static InputModel RelevanceModel(string answer, string context) => new(EvalType.Relevance, new KernelArguments
        {
            ["answer"] = answer,
            ["context"] = context
        });
        public static InputModel CoherenceModel(string answer, string question) => new(EvalType.Coherence, new KernelArguments
        {
            ["answer"] = answer,
            ["question"] = question
        });
    }
    public enum EvalType
    {
        GptGroundedness,
        GptSimilarity,
        Relevance,
        Coherence
    }
   
}
