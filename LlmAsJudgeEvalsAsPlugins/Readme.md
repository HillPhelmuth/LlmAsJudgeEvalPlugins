# LlmAsJudgeEvals

This library provides a service for evaluating responses from Large Language Models (LLMs) using the LLM itself as a judge. It leverages [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview/) to define and execute evaluation functions based on prompt templates. 

**For a more precise evaluation score, the library utilizes `logprobs` and calculates a weighted total of probabilities for each evaluation criterion.**

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

* **Groundedness:** Evaluates factual accuracy and support in context
* **Groundedness2:** Alternative groundedness evaluation with for whether answer logically follows from the context
* **Similarity:** Measures response similarity to reference text
* **Relevance:** Assesses response relevance to prompt/question
* **Coherence:** Evaluates logical flow and consistency
* **Perceived Intelligence:** Rates apparent knowledge and reasoning (with/without RAG)
* **Fluency:** Measures natural language quality
* **Empathy:** Assesses emotional understanding
* **Helpfulness:** Evaluates practical value of response
* **Retrieval:** Evaluates the retrieved content based on the query
* **Role Adherence:** Measures how well the response maintains the persona, style, and constraints specified in the instructions or assigned role
* **Excessive Agency:** Evaluates whether the response exhibits behaviors that go beyond the intended scope, permissions, or safeguards of the LLM (e.g., excessive autonomy, permissions, or functionality)

#### Agentic Evaluation Functions

* **Intent Resolution:** Measures how well the agent identifies and clarifies user intent, including asking for clarifications and staying within scope
* **Tool Call Accuracy:** Measures the agent’s proficiency in selecting appropriate tools, and accurately extracting and processing inputs
* **Task Adherence:** Measures how well the agent’s final response meets the predefined goal or request specified in the task

Each function has an "Explain" variant (e.g., GroundednessExplain, CoherenceExplain) that provides:
- Numerical score
- Detailed reasoning
- Chain-of-thought analysis
- Probability-weighted score

### Initialize the EvalService
**Using a `Kernel` instance:**
```csharp
// Initialize the Semantic Kernel
var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion("openai-model-name", "openai-apiKey").Build();

// Create an instance of the EvalService
var evalService = new EvalService(kernel);
```
**Using an `Microsoft.Extensions.AI.IChatClient` instance:**
```csharp
// Initialize the Chat Client
var chatClient = new OpenAIClient("openai-apiKey").GetChatClient("model-name").AsIChatClient();

// Create an instance of the EvalService
var evalService = new EvalService(chatClient);
```

### Execute Evaluations
```csharp

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

## Using KernelPlugin Directly (Alternative to EvalService for Built-in Evals)

You can use the evaluation functions directly by importing the plugin with `ImportEvalPlugin` and invoking functions via the kernel. This is an alternative to using `EvalService`.

```csharp
// Initialize the Semantic Kernel
var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion("openai-model-name", "openai-apiKey").Build();

// Import the evaluation plugin (loads all built-in eval functions)
var evalPlugin = kernel.ImportEvalPlugin();

// Prepare input arguments for the function
var arguments = new KernelArguments
{
    ["input"] = "This is the answer to evaluate.",
    ["question"] = "This is the question or prompt that generated the answer."
};

// Get the 'Coherence' evaluation function from the plugin
var coherenceFunction = evalPlugin["Coherence"];

// Invoke the 'Coherence' evaluation function directly
var result = await kernel.InvokeAsync(coherenceFunction, arguments);

Console.WriteLine($"Coherence score: {result.GetValue<int>()}");
```

You can replace `"Coherence"` with any other built-in evaluation function name. The plugin name defaults to `EvalPlugin` unless specified otherwise.

## Features

* **Define evaluation functions using prompt templates:** You can define evaluation functions using prompt templates written in YAML. 
* **Execute evaluations:** The `EvalService` provides methods for executing evaluations on input data.
* **Score Plus Explanation:** Get detailed explanations and chain-of-thought reasoning along with scores.
* **Aggregate results:** The `EvalService` can aggregate evaluation scores across multiple inputs.
* **Built-in evaluation functions:** Pre-defined functions for common evaluation metrics.
* **Logprobs-based scoring:** Leverages `logprobs` for a more granular and precise evaluation score, when available.
