#region References

using Cornerstone.Avalonia.Serialization.Json;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Avalonia.Serialization;

public static class CornerstoneAvaloniaSerializerConfigurator
{
	#region Fields

	private static bool _configured;

	#endregion

	#region Methods

	public static void Configure()
	{
		if (_configured)
		{
			return;
		}
		_configured = true;
		Serializer.AddTypeInfoResolvers(
			CornerstoneAvaloniaJsonSerializerContext.Default
		);
		Serializer.SerializationOptions.Converters.Add(new SplitFractionsJsonConverter());
	}

	#endregion
}