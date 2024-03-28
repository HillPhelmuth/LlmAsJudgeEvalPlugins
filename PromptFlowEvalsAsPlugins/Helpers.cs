using Microsoft.SemanticKernel;
using System.Reflection;
using System.Text.Json;

namespace PromptFlowEvalsAsPlugins;

internal static class Helpers
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
}