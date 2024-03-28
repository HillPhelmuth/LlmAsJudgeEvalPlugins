using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using static PromptFlowEvalsAsPlugins.Helpers;

namespace PromptFlowEvalsAsPlugins;

public static class Extentions
{
    /// <summary>
    /// Imports an evaluation plugin into the kernel.
    /// This can be used instead of <see cref="EvalService"/>
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <param name="pluginName">The name of the plugin.</param>
    /// <param name="description">The description of the plugin (optional).</param>
    /// <returns>The imported kernel plugin.</returns>
    public static KernelPlugin ImportEvalPlugin(this Kernel kernel, string pluginName = "EvalPlugin", string? description = null)
    {
        var plugin = kernel.CreateEvalPlugin(pluginName, description);
        kernel.Plugins.Add(plugin);
        return plugin;
    }

    /// <summary>
    /// Creates an evaluation plugin from prompt YAML files.
    /// This can be used instead of <see cref="EvalService"/>
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <param name="pluginName">The name of the plugin.</param>
    /// <param name="description">The description of the plugin (optional).</param>
    /// <returns>The created kernel plugin.</returns>
    public static KernelPlugin CreateEvalPlugin(this Kernel kernel, string pluginName, string? description)
    {
        var files = Enum.GetNames<EvalType>();
        var kFunctions = new List<KernelFunction>();
        foreach (var file in files)
        {
            var yamlText = ExtractFromAssembly<string>($"{file}.yaml");
            try
            {
                var func = kernel.CreateFunctionFromPromptYaml(yamlText);
                kFunctions.Add(func);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR DESERIALIZING YAML\n\nFunction: {Path.GetFileName(file)}\n\n{ex}");
            }
        }
        var plugin = KernelPluginFactory.CreateFromFunctions(pluginName, description, kFunctions);
        return plugin;
    }
    public static T? GetTypedResult<T>(this FunctionResult functionResult)
    {
        if (functionResult.ValueType == typeof(T))
        {
            return functionResult.GetValue<T>()!;
        }
        var stringValue = functionResult.GetValue<string>();
        try
        {
            var json = stringValue!.Replace("```json", "").Replace("```", "").Trim();
            var obj = JsonSerializer.Deserialize<T>(json, JsonOptionsCaseInsensitive);
            return obj;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return default;
        }
    }
}