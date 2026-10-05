#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Data;

public static class CloneableExtensions
{
	#region Methods

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "GetType() cannot flow DynamicallyAccessedMembers; clone uses generated UpdateWith.")]
	public static object DeepCloneUsingUpdateWith(this object value, int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		if (value == null)
		{
			return null;
		}

		var type = value.GetType();
		settings ??= IncludeExcludeSettings.Empty;
		return DeepCloneUsingUpdateWith(value, type, maxDepth, settings);
	}

	public static object DeepCloneUsingUpdateWith(this object value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type, int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		var response = Activator.CreateInstance(type);
		settings ??= IncludeExcludeSettings.Empty;

		switch (response)
		{
			case IUpdateable updateable:
			{
				var allOptions = Cache.GetSettings(type, UpdateableAction.Updateable).WithMoreSettings(settings);
				updateable.UpdateWith(value, allOptions);
				break;
			}
			default:
			{
				throw new NotSupportedException();
			}
		}

		if (response is ITrackPropertyChanges changeable)
		{
			changeable.ResetHasChanges();
		}

		return response;
	}

	#endregion
}