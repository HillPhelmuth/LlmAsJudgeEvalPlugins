using System.Reflection;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace HillPhelmuth.SemanticKernel.LlmAsJudgeEvals;

public static class Helpers
{
    internal static T ExtractFromAssembly<T>(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var jsonName = assembly.GetManifestResourceNames()
            .SingleOrDefault(s => s.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) ?? "";
        using var stream = assembly.GetManifestResourceStream(jsonName);
        using var reader = new StreamReader(stream);
        object result = reader.ReadToEnd();
        if (typeof(T) == typeof(string))
            return (T)result;
        return JsonSerializer.Deserialize<T>(result.ToString());
    }
    internal static readonly JsonSerializerOptions JsonOptionsCaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
    internal static Dictionary<string, KernelFunction> GetFunctionsFromYaml()
    {
        var files = Enum.GetNames<EvalType>();
        var result = new Dictionary<string, KernelFunction>();
        foreach (var file in files)
        {
            var yamlText = ExtractFromAssembly<string>($"{file}.yaml");
            var function = KernelFunctionYaml.FromPromptYaml(yamlText);
            result.TryAdd(file, function);
        }
        return result;
    }
    /// <summary>
    /// Retrieves prompt template configurations from embedded YAML resources for each <see cref="EvalType"/>.
    /// </summary>
    /// <param name="pluginName">
    /// The name of the plugin to associate with the prompt templates. Defaults to "EvalPlugin".
    /// </param>
    /// <returns>
    /// A dictionary mapping each <see cref="EvalType"/> name to its corresponding <see cref="PromptTemplateConfig"/>.
    /// </returns>
    public static Dictionary<string, PromptTemplateConfig> GetPromptTemplateConfigs(string pluginName = "EvalPlugin")
    {
        var files = Enum.GetNames<EvalType>();
        var result = new Dictionary<string, PromptTemplateConfig>();
        foreach (var file in files)
        {
            var yamlText = ExtractFromAssembly<string>($"{file}.yaml");
            PromptTemplateConfig config = KernelFunctionYaml.ToPromptTemplateConfig(yamlText);
            result.Add(file, config);
        }
        return result;
    }
}