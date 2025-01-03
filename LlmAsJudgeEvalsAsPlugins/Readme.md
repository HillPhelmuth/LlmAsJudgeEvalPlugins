# LlmAsJudgeEvals

This library provides a service for evaluating responses from Large Language Models (LLMs) using the LLM itself as a judge. It leverages Semantic Kernel to define and execute evaluation functions based on prompt templates.

## Installation

Install the package via NuGet:

```
nuget install HillPhelmuth.SemanticKernel.LlmAsJudgeEvals
```

## Usage

### Built-in Evaluation Functions

```csharp
// Initialize the Semantic Kernel
var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion("openai-model-name", "openai-apiKey").Build();

// Create an instance of the EvalService
var evalService = new EvalService(kernel);

// Create an input model for the built-in evaluation function
var coherenceInput = InputModel.CoherenceModel("This is the answer to evaluate.", "This is the question or prompt that generated the answer");

// Execute the evaluation
var result = await evalService.ExecuteEval(inputModel);

Console.WriteLine($"Evaluation score: {result.Score}");

// Execute evaluation with explanation
var resultWithExplanation = await evalService.ExecuteScorePlusEval(inputModel);

Console.WriteLine($"Score: {resultWithExplanation.Score}");
Console.WriteLine($"Reasoning: {resultWithExplanation.Reasoning}");
Console.WriteLine($"Chain of Thought: {resultWithExplanation.ChainOfThought}");
```

### Custom Evaluation Functions

```csharp
// Initialize the Semantic Kernel
var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion("openai-model-name", "openai-apiKey").Build();

// Create an instance of the EvalService
var evalService = new EvalService(kernel);

// Add an evaluation function (optional)
evalService.AddEvalFunction("MyEvalFunction", "This is the prompt for my evaluation function.", new PromptExecutionSettings());

// Create an input model for the evaluation function
var inputModel = new InputModel
{
    FunctionName = "MyEvalFunction", // Replace with the name of your evaluation function
    RequiredInputs = new Dictionary<string, string>
    {
        { "input", "This is the text to evaluate." }
    }
};

// Execute the evaluation
var result = await evalService.ExecuteEval(inputModel);

Console.WriteLine($"Evaluation score: {result.Score}");
```

## Features

* **Define evaluation functions using prompt templates:**  You can define evaluation functions using prompt templates written in YAML. 
* **Execute evaluations:** The `EvalService` provides methods for executing evaluations on input data.
* **Score Plus Explanation:** Use `ExecuteScorePlusEval` to get detailed explanations and chain-of-thought reasoning along with scores.
* **Aggregate results:**  The `EvalService` can aggregate evaluation scores across multiple inputs.
* **Built-in evaluation functions:** The package includes pre-defined evaluation functions for:
  - Groundedness (1-5 score)
  - Groundedness2 (1-10 score)
  - Similarity
  - Relevance
  - Coherence
  - Perceived Intelligence (with and without RAG context)
  - Fluency
  - Empathy
  - Helpfulness

Each evaluation function has a corresponding "Explain" version that provides detailed explanations and chain-of-thought reasoning along with the score. For example:
- GroundednessExplain
- CoherenceExplain
- SimilarityExplain
etc.

These evaluation functions can be easily accessed using the InputModel factory methods:
```csharp
var coherenceInput = InputModel.CoherenceModel(answer, question);
var groundednessInput = InputModel.GroundednessModel(answer, question, context);
var coherenceWithExplanationInput = InputModel.CoherenceExplainModel(answer, question);
```

## Example of Score Plus Explanation Output

```json
{
    "EvalName": "CoherenceExplain",
    "Score": 4,
    "Reasoning": "The answer is mostly coherent with good flow and clear organization. It addresses the question directly and maintains logical connections between ideas.",
    "ChainOfThought": "1. First, I examined how the sentences connect\n2. Checked if ideas flow naturally\n3. Verified if the response stays focused on the question\n4. Assessed overall clarity and organization\n5. Considered natural language use",
    "ProbScore": 3.92
}
```



