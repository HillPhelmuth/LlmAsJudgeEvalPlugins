# LlmAsJudgeEvals

This library provides a service for evaluating responses from Large Language Models (LLMs) using the LLM itself as a judge. It leverages Semantic Kernel to define and execute evaluation functions based on prompt templates. 

**For a more precise evaluation score, the library utilizes `logprobs` and calculates a weighted total of probabilities for each evaluation criterion.**

Check out the [LLM-as-judge Demo](https://promptflowevalsaspluginsdemo.azurewebsites.net/)

## Installation

Install the package via NuGet:

**powershell:**
```
Install-Package HillPhelmuth.SemanticKernel.LlmAsJudgeEvals
```
**dotnet cli:**
```
dotnet add package HillPhelmuth.SemanticKernel.LlmAsJudgeEvals
```

## Usage

### Built-in Evaluation Functions

The package includes a comprehensive set of built-in evaluation functions, each with an accompanying "Explain" version that provides detailed reasoning:

* **Groundedness (1-5):** Evaluates factual accuracy and support in context
* **Groundedness2 (1-10):** Alternative groundedness evaluation with finer granularity
* **Similarity:** Measures response similarity to reference text
* **Relevance:** Assesses response relevance to prompt/question
* **Coherence:** Evaluates logical flow and consistency
* **Perceived Intelligence:** Rates apparent knowledge and reasoning (with/without RAG)
* **Fluency:** Measures natural language quality
* **Empathy:** Assesses emotional understanding
* **Helpfulness:** Evaluates practical value of response
* **Retrieval:** Evaluates the retrieved content based on the query
* **Role Adherence (1-5):** Measures how well the response maintains the persona, style, and constraints specified in the instructions or assigned role
* **Excessive Agency (1-5):** Evaluates whether the response exhibits behaviors that go beyond the intended scope, permissions, or safeguards of the LLM (e.g., excessive autonomy, permissions, or functionality)

Each function has an "Explain" variant (e.g., GroundednessExplain, CoherenceExplain) that provides:
- Numerical score
- Detailed reasoning
- Chain-of-thought analysis
- Probability-weighted score

```csharp
// Initialize the Semantic Kernel
var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion("openai-model-name", "openai-apiKey").Build();

// Create an instance of the EvalService
var evalService = new EvalService(kernel);

// Create an input model for the built-in evaluation function
var coherenceInput = InputModel.CoherenceModel("This is the answer to evaluate.", "This is the question or prompt that generated the answer");

// Execute the evaluation
var result = await evalService.ExecuteEval(coherenceInput);

Console.WriteLine($"Evaluation score: {result.Score}");

// Execute evaluation with detailed explanation
var resultWithExplanation = await evalService.ExecuteScorePlusEval(inputModel);

Console.WriteLine($"Score: {resultWithExplanation.Score}");
Console.WriteLine($"Reasoning: {resultWithExplanation.Reasoning}");
Console.WriteLine($"Chain of Thought: {resultWithExplanation.ChainOfThought}");
```

### Factory Methods for Easy Access

```csharp
var coherenceInput = InputModel.CoherenceModel(answer, question);
var groundednessInput = InputModel.GroundednessModel(answer, question, context);
var coherenceWithExplanationInput = InputModel.CoherenceExplainModel(answer, question);
```

### Example Output (Score Plus Explanation)

```json
{
    "EvalName": "CoherenceExplain",
    "Score": 4,
    "Reasoning": "The answer is mostly coherent with good flow and clear organization. It addresses the question directly and maintains logical connections between ideas.",
    "ChainOfThought": "1. First, I examined how the sentences connect\n2. Checked if ideas flow naturally\n3. Verified if the response stays focused on the question\n4. Assessed overall clarity and organization\n5. Considered natural language use",
    "ProbScore": 3.92
}
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

* **Define evaluation functions using prompt templates:** You can define evaluation functions using prompt templates written in YAML. 
* **Execute evaluations:** The `EvalService` provides methods for executing evaluations on input data.
* **Score Plus Explanation:** Get detailed explanations and chain-of-thought reasoning along with scores.
* **Aggregate results:** The `EvalService` can aggregate evaluation scores across multiple inputs.
* **Built-in evaluation functions:** Pre-defined functions for common evaluation metrics.
* **Logprobs-based scoring:** Leverages `logprobs` for a more granular and precise evaluation score.

