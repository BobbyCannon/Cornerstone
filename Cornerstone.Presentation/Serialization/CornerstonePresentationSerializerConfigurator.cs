#region References

using Cornerstone.Presentation.Serialization.Json;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Presentation.Serialization;

public static class CornerstonePresentationSerializerConfigurator
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
			CornerstonePresentationJsonSerializerContext.Default
		);
		Serializer.SerializationOptions.Converters.Add(new SplitFractionsJsonConverter());
	}

	#endregion
}