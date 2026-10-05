#region References

using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Agent.Serialization;

public static class AppSerializerConfigurator
{
	#region Fields

	private static bool _configured;

	#endregion

	#region Methods

	internal static void Configure()
	{
		if (_configured)
		{
			return;
		}
		_configured = true;
		Serializer.AddTypeInfoResolvers(
			AppSerializerContext.Default
		);
	}

	#endregion
}