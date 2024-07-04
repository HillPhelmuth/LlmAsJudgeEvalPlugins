using Microsoft.SemanticKernel;
using System.Reflection;
using System.Text.Json;

namespace PromptFlowEvalsAsPlugins;

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
    public static byte[] ReadFromAssembly(string fileName)
    {
	    var assembly = Assembly.GetExecutingAssembly();
	    var jsonName = assembly.GetManifestResourceNames()
		    .SingleOrDefault(s => s.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) ?? "";
	    using var stream = assembly.GetManifestResourceStream(jsonName);
	    using var memoryStream = new MemoryStream();
	    stream.CopyTo(memoryStream);
	    return memoryStream.ToArray();
    }
}