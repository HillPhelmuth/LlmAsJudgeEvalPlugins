using System.Reflection;
using Tiktoken;

namespace PromptFlowEvalsAsPlugins.Demo;

public class StringHelpers
{
	static Encoder? _encoder;
	public static int GetTokens(string text)
	{
		_encoder ??= ModelToEncoder.For("gpt-3.5-turbo");
			
		return _encoder.CountTokens(text); 
	}
}
public class FileHelpers
{
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