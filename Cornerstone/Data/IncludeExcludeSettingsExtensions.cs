#region References

using System;
using Cornerstone.Internal;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Include / Exclude settings for filtering.
/// </summary>
public static class IncludeExcludeSettingsExtensions
{
	#region Methods

	/// <summary>
	/// Gets the action settings for an entity.
	/// </summary>
	/// <param name="entity"> The entity. </param>
	/// <param name="actionType"> The updatable action. </param>
	/// <returns> The settings for the action entity combination. </returns>
	public static IncludeExcludeSettings GetIncludeExcludeSettings<T>(this T entity, UpdateableAction actionType)
		where T : class, IEntity
	{
		return Cache.GetSettings(entity.GetRealType(), actionType);
	}

	/// <summary>
	/// Gets the action settings for an entity.
	/// </summary>
	/// <param name="entityType"> </param>
	/// <param name="actionType"> </param>
	/// <returns> </returns>
	public static IncludeExcludeSettings GetIncludeExcludeSettings(this Type entityType, UpdateableAction actionType)
	{
		return Cache.GetSettings(entityType, actionType);
	}

	/// <summary>
	/// Return update settings with only exclusions.
	/// </summary>
	/// <param name="exclusions"> The exclusions. </param>
	/// <returns> The settings with only exclusions. </returns>
	public static IncludeExcludeSettings ToOnlyExcludingSettings(this string[] exclusions)
	{
		return new IncludeExcludeSettings(null, exclusions);
	}

	/// <summary>
	/// Return update settings with only including.
	/// </summary>
	/// <param name="including"> The including. </param>
	/// <returns> The settings with only inclusions. </returns>
	public static IncludeExcludeSettings ToOnlyIncludingSettings(this string[] including)
	{
		return new IncludeExcludeSettings(including, null);
	}

	#endregion
}