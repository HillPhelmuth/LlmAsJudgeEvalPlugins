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



