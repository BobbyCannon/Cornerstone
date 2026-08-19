#region References

using System;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Fluent configuration for mapping properties between an off-dispatcher model
/// (<see cref="Data.ITrackPropertyChanges" />) and a <see cref="DispatchableViewModel" />.
/// Supports different property names and types via converters; optional two-way flow.
/// </summary>
public interface IPropertyMap
{
	#region Methods

	/// <summary>
	/// One-way: model → view when names and types match (identity conversion).
	/// </summary>
	IPropertyMap MapOneWay(string propertyName);

	/// <summary>
	/// One-way: model → view. View changes are not written back.
	/// </summary>
	IPropertyMap MapOneWay<TModelValue, TViewValue>(
		string modelPropertyName,
		string viewPropertyName,
		Func<TModelValue, TViewValue> toView);

	/// <summary>
	/// Two-way when names and types match (identity conversion).
	/// </summary>
	IPropertyMap MapTwoWay(string propertyName);

	/// <summary>
	/// Two-way when types match but names may differ (identity conversion).
	/// </summary>
	IPropertyMap MapTwoWay(string modelPropertyName, string viewPropertyName);

	/// <summary>
	/// Two-way with converters (different names and/or types).
	/// </summary>
	IPropertyMap MapTwoWay<TModelValue, TViewValue>(
		string modelPropertyName,
		string viewPropertyName,
		Func<TModelValue, TViewValue> toView,
		Func<TViewValue, TModelValue> toModel);

	#endregion
}
