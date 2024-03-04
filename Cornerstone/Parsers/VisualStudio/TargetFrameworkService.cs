#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Attributes;
using Cornerstone.Extensions;

#endregion

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cornerstone.Parsers.VisualStudio;

public static class TargetFrameworkService
{
	#region Fields

	private static readonly ConcurrentDictionary<string, TargetFramework[]> _ancestors;
	private static readonly ConcurrentDictionary<string, TargetFrameworkAttribute> _attributesByMoniker;
	private static readonly ConcurrentDictionary<TargetFrameworkType, TargetFrameworkAttribute> _attributesByType;
	private static readonly ConcurrentDictionary<TargetFrameworkType, TargetFramework> _baseFrameworksByType;
	private static readonly ConcurrentDictionary<string, TargetFramework[]> _descendants;
	private static readonly ConcurrentDictionary<string, TargetFramework[]> _frameworkByCustomGroup;
	private static readonly ConcurrentDictionary<string, TargetFramework> _frameworksByMoniker;

	#endregion

	#region Constructors

	static TargetFrameworkService()
	{
		_ancestors = new ConcurrentDictionary<string, TargetFramework[]>(StringComparer.OrdinalIgnoreCase);
		_descendants = new ConcurrentDictionary<string, TargetFramework[]>(StringComparer.OrdinalIgnoreCase);
		_attributesByType = EnumExtensions.GetValues<TargetFrameworkType>()
			.Where(x => x != TargetFrameworkType.Unknown)
			.ToConcurrentDictionary(x => x, EnumExtensions.GetAttribute<TargetFrameworkType, TargetFrameworkAttribute>);
		_attributesByMoniker = _attributesByType.Values.ToConcurrentDictionary(x => x.Moniker, x => x, StringComparer.OrdinalIgnoreCase);
		_baseFrameworksByType = _attributesByType.ToConcurrentDictionary(x => x.Key, x => TargetFramework.InternalCreateTargetFramework(x.Key, x.Value));

		// These collection changes as target frameworks are added.
		_frameworksByMoniker = new ConcurrentDictionary<string, TargetFramework>(StringComparer.OrdinalIgnoreCase);
		_frameworkByCustomGroup = new ConcurrentDictionary<string, TargetFramework[]>(StringComparer.OrdinalIgnoreCase);

		AddToCoreList(_baseFrameworksByType.Values.ToArray());
	}

	#endregion

	#region Methods

	public static TargetFramework[] GetAncestors(TargetFrameworkType targetFrameworkType)
	{
		return _baseFrameworksByType.TryGetValue(targetFrameworkType, out var targetFramework)
			? GetAncestors(targetFramework)
			: [];
	}

	public static TargetFramework[] GetAncestors(TargetFramework targetFramework)
	{
		return _ancestors.GetOrAdd(targetFramework.Moniker,
			_ =>
			{
				var response = new List<TargetFramework>();
				var currentFramework = targetFramework;

				while (currentFramework.Parent != null)
				{
					response.Add(currentFramework.Parent);
					currentFramework = currentFramework.Parent;
				}

				response.Reverse();
				return response.ToArray();
			});
	}

	/// <summary>
	/// Get best net standard framework.
	/// </summary>
	/// <param name="frameworks"> The frameworks to process. </param>
	/// <returns> The best net standard framework. </returns>
	public static TargetFramework GetBestNetStandardFramework(IEnumerable<TargetFramework> frameworks)
	{
		return null;
	}

	/// <summary>
	/// Get the target framework from the type.
	/// </summary>
	/// <param name="type"> The framework type. </param>
	/// <returns> The target framework. </returns>
	public static TargetFramework GetByType(TargetFrameworkType type)
	{
		return _baseFrameworksByType.GetOrDefault(type);
	}

	/// <summary>
	/// Find the intersecting latest compatible framework from the two collections.
	/// </summary>
	/// <param name="current"> The current frameworks. </param>
	/// <param name="suggested"> The suggested frameworks. </param>
	/// <returns> The found latest framework that existing in both groups otherwise null. </returns>
	public static TargetFramework GetCompatibleFramework(IList<TargetFramework> current, IList<TargetFramework> suggested)
	{
		if (current.Count <= 0)
		{
			return null;
		}

		var currentOrdered = OrderFrameworks(current);
		var suggestedOrdered = OrderFrameworks(suggested);

		foreach (var suggestedFramework in suggestedOrdered)
		{
			// Check for the exact match.
			if (currentOrdered.Contains(suggestedFramework))
			{
				// This is an exact match so just return the exact type.
				return suggestedFramework;
			}

			// Now find the closest framework minus the operating system version.
			var nonVersioned = currentOrdered.FirstOrDefault(x => x.Type == suggestedFramework.Type);
			if (nonVersioned != null)
			{
				// This is match based on everything but versioned.
				return suggestedFramework;
			}

			// Now find the closest framework minus the operating system version.
			var platformed = currentOrdered.FirstOrDefault(x => x.Platform == suggestedFramework);
			if (platformed != null)
			{
				// This is match based on platform.
				return suggestedFramework;
			}

			// See if this net standard framework is supported.
			if (suggestedFramework.IsNetStandard)
			{
				// Now find the closest framework supported .net standard
				var netstandard = currentOrdered
					.FirstOrDefault(x =>
						(x.NetStandard != null)
						&& (
							// Check for exact match
							(x.NetStandard == suggestedFramework)
							// See if the net standard framework is a lesser framework
							|| GetDescendants(x.NetStandard).Contains(suggestedFramework)
						)
					);
				if (netstandard != null)
				{
					// We found a net standard version that is supported.
					return suggestedFramework;
				}
			}
		}

		return null;
	}

	public static TargetFramework[] GetDescendants(TargetFrameworkType targetFrameworkType)
	{
		return _baseFrameworksByType.TryGetValue(targetFrameworkType, out var targetFramework)
			? GetDescendants(targetFramework)
			: [];
	}

	public static TargetFramework[] GetDescendants(TargetFramework targetFramework)
	{
		return _descendants.GetOrAdd(targetFramework.Moniker,
			_ =>
			{
				var response = new List<TargetFramework>();
				GetChildren(targetFramework, response);
				return response.ToArray();
			});
	}

	public static TargetFramework[] GetNetStandardFrameworks()
	{
		return _frameworkByCustomGroup.GetOrAdd(
			nameof(TargetFrameworkAttribute.NetStandard),
			_ =>
			{
				return _frameworksByMoniker
					.Where(x => x.Value.IsNetStandard)
					.Select(x => x.Value)
					.ToArray();
			}
		);
	}

	public static TargetFramework GetOrAddFramework(string moniker)
	{
		if (_frameworksByMoniker.TryGetValue(moniker, out var framework)
			|| TryGetClassicProjectsByVersion(moniker, out framework))
		{
			return framework;
		}

		framework = new TargetFramework();
		framework.ParseMoniker(moniker);
		AddToCoreList(framework);
		return framework;
	}

	public static string GetPlatformMoniker(TargetFrameworkType platformType)
	{
		return _attributesByType.GetOrDefault(platformType)?.Moniker ?? string.Empty;
	}

	public static IList<TargetFramework> OrderFrameworks(IEnumerable<TargetFramework> current)
	{
		return current
			.OrderByDescending(x => x.Type)
			.ThenByDescending(x => x.OperatingSystemVersion)
			.ToList();
	}

	public static bool Supports(ITargetFrameworkMoniker current, ITargetFrameworkMoniker suggestion)
	{
		if ((current == null) || (suggestion == null))
		{
			return false;
		}

		// https://dotnet.microsoft.com/en-us/platform/dotnet-standard#versions
		// https://nugettools.azurewebsites.net/6.8.0/framework-compatibility

		//if (current.FrameworkType == suggestion.FrameworkType)
		//{
		//	return true;
		//}

		//if ((current.Attribute != null) && (current.Attribute.NetStandard != TargetFrameworkType.Unknown))
		//{
		//	if (suggestion.FrameworkType == current.Attribute.NetStandard)
		//	{
		//		return true;
		//	}

		//	var supportedStandards = GetDescendants(current.Attribute.NetStandard);
		//	if (supportedStandards.Any(x => x.FrameworkType == current.Attribute.NetStandard))
		//	{
		//		return true;
		//	}
		//}

		//var descendants = GetDescendants(current.FrameworkType);
		//if (descendants.Any(x => x.FrameworkType == suggestion.FrameworkType))
		//{
		//	return true;
		//}

		return false;
	}

	private static void AddToCoreList(params TargetFramework[] targetFrameworks)
	{
		foreach (var targetFramework in targetFrameworks)
		{
			_frameworksByMoniker.TryAdd(targetFramework.Moniker, targetFramework);
		}

		foreach (var targetFramework in targetFrameworks)
		{
			// Try to find the parent attribute
			if (_attributesByMoniker.TryGetValue(targetFramework.Moniker, out var attribute))
			{
				if ((targetFramework.Parent == null)
					&& (attribute.Parent != TargetFrameworkType.Unknown)
					&& _baseFrameworksByType.TryGetValue(attribute.Parent, out var parentFramework))
				{
					targetFramework.Parent = parentFramework;
				}

				if ((targetFramework.Platform == null)
					&& (attribute.Platform != TargetFrameworkType.Unknown)
					&& _baseFrameworksByType.TryGetValue(attribute.Platform, out var platformFramework))
				{
					targetFramework.Platform = platformFramework;
				}
				
				if ((targetFramework.NetStandard == null)
					&& (attribute.NetStandard != TargetFrameworkType.Unknown)
					&& _baseFrameworksByType.TryGetValue(attribute.NetStandard, out var standardFramework))
				{
					targetFramework.NetStandard = standardFramework;
				}
			}

			if ((targetFramework.Platform == null)
				&& !string.IsNullOrWhiteSpace(targetFramework.OperatingSystem)
				&& _frameworksByMoniker.TryGetValue(targetFramework.PlatformMoniker, out var platformFramework2))
			{
				targetFramework.Platform = platformFramework2;
			}

			if ((targetFramework.Type == TargetFrameworkType.Unknown)
				&& _frameworksByMoniker.TryGetValue(targetFramework.TypeMoniker, out var typeFramework))
			{
				targetFramework.Type = typeFramework.Type;
			}
		}
	}

	private static void GetChildren(TargetFramework parent, ICollection<TargetFramework> collection)
	{
		if (parent.Children == null)
		{
			return;
		}

		var children = parent.Children;
		collection.AddRange(children);

		foreach (var child in children)
		{
			GetChildren(child, collection);
		}
	}

	private static bool TryGetClassicProjectsByVersion(string version, out TargetFramework targetFramework)
	{
		// https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-target-framework-and-target-platform?view=vs-2022
		// The available values for TargetFrameworkVersion are v2.0, v3.0, v4.0, v3.5, v4.5.2, v4.6, v4.6.1, v4.6.2, v4.7, v4.7.1, v4.7.2, and v4.8.
		targetFramework = version switch
		{
			"v2.0" => _baseFrameworksByType[TargetFrameworkType.Net20],
			"v3.5" => _baseFrameworksByType[TargetFrameworkType.Net35],
			"v4.0" => _baseFrameworksByType[TargetFrameworkType.Net40],
			"v4.5.2" => _baseFrameworksByType[TargetFrameworkType.Net452],
			"v4.6" => _baseFrameworksByType[TargetFrameworkType.Net46],
			"v4.6.1" => _baseFrameworksByType[TargetFrameworkType.Net461],
			"v4.6.2" => _baseFrameworksByType[TargetFrameworkType.Net462],
			"v4.7" => _baseFrameworksByType[TargetFrameworkType.Net47],
			"v4.7.1" => _baseFrameworksByType[TargetFrameworkType.Net471],
			"v4.7.2" => _baseFrameworksByType[TargetFrameworkType.Net472],
			"v4.8" => _baseFrameworksByType[TargetFrameworkType.Net48],
			_ => null
		};

		return targetFramework != null;
	}

	#endregion
}