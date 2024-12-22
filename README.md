
# LlmAsJudgeEvals

This library provides a service for evaluating responses from Large Language Models (LLMs) using the LLM itself as a judge. It leverages Semantic Kernel to define and execute evaluation functions based on prompt templates. 

**For a more precise evaluation score, the library utilizes `logprobs` and calculates a weighted total of probabilities for each evaluation criterion.**

## Installation

Install the package via NuGet:

```
nuget install HillPhelmuth.SemanticKernel.LlmAsJudgeEvals
```

## Usage

### Built-in Evaluation Functions

The package includes a set of built-in evaluation functions, each focusing on a specific aspect of LLM output quality:

* **Coherence:** Evaluates the logical flow and consistency of the response.
* **Empathy:** Assesses the level of empathy and understanding conveyed in the response.
* **Fluency:** Measures the smoothness and naturalness of the language used.
* **GptGroundedness:** Determines how well the response is grounded in factual information.
* **GptGroundedness2:** An alternative approach to evaluating groundedness.
* **GptSimilarity:**  Compares the response to a reference text or objectively correct answer for similarity.
* **Helpfulness:**  Assesses the degree to which the response is helpful and informative.
* **PerceivedIntelligence:** Evaluates the perceived intelligence and knowledge reflected in the response.
* **PerceivedIntelligenceNonRag:** A variant of PerceivedIntelligence tailored for non-Retrieval Augmented Generation (RAG) models.
* **Relevance:** Measures the relevance of the response to the given prompt or question and a reference text for RAG.


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
* **Aggregate results:**  The `EvalService` can aggregate evaluation scores across multiple inputs.
* **Built-in evaluation functions:** The package includes a set of pre-defined evaluation functions based on common evaluation metrics.
* **Logprobs-based scoring:** Leverages `logprobs` for a more granular and precise evaluation score.

