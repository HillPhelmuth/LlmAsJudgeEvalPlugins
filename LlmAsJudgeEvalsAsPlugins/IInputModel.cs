using Microsoft.SemanticKernel;

namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

public interface IInputModel
{
    /// <summary>
    /// Gets the Semantic Kernel function name.
    /// </summary>
    string FunctionName { get; }

    /// <summary>
    /// Gets the required KernelArguments inputs.
    /// </summary>
    KernelArguments RequiredInputs { get; }
}