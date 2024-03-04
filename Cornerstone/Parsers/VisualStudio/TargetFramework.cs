#region References

using Cornerstone.Attributes;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Parsers.VisualStudio;

/// <summary>
/// Represents the .NET project target framework.
/// </summary>
/// <remarks>
/// For further details on target frameworks see:
/// https://learn.microsoft.com/en-us/dotnet/standard/frameworks
/// https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-1-0
/// </remarks>
public class TargetFramework : TargetFrameworkMoniker
{
	#region Properties

	/// <summary>
	/// True if this target framework is a classic net framework.
	/// </summary>
	public bool IsNetFramework { get; set; }

	/// <summary>
	/// True if this target framework is a NetStandard framework.
	/// </summary>
	public bool IsNetStandard { get; set; }

	/// <summary>
	/// True if this target framework is a UAP framework.
	/// </summary>
	public bool IsUniversalFramework { get; set; }

	/// <summary>
	/// The net standard version supported in this framework.
	/// </summary>
	public TargetFramework NetStandard { get; set; }

	/// <summary>
	/// On option platform target framework.
	/// </summary>
	public TargetFramework Platform { get; set; }

	/// <summary>
	/// An option list of children frameworks.
	/// </summary>
	internal SpeedyList<TargetFramework> Children { get; set; }

	/// <summary>
	/// The optional parent target framework.
	/// </summary>
	internal TargetFramework Parent { get; set; }

	/// <summary>
	/// The core type (un-versioned) for sorting.
	/// </summary>
	internal TargetFrameworkType Type { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(Parent):
			{
				Parent.Children ??= new SpeedyList<TargetFramework>();
				Parent.Children.Add(this);
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	/// <summary>
	/// This is used in the constructor to build the initial list.
	/// Notes
	/// - This is the core list.
	/// - Do not use this method other than once in the constructor.
	/// - Do not use any other resources in this class.
	/// </summary>
	internal static TargetFramework InternalCreateTargetFramework(TargetFrameworkType type, TargetFrameworkAttribute attribute)
	{
		var response = new TargetFramework
		{
			IsNetFramework = attribute.IsNetFramework,
			IsNetStandard = attribute.IsNetStandard,
			IsUniversalFramework = attribute.IsUniversal,
			Moniker = attribute.Moniker,
			OperatingSystem = attribute.OperatingSystem ?? string.Empty,
			OperatingSystemVersion = string.Empty,
			PlatformMoniker = TargetFrameworkService.GetPlatformMoniker(attribute.Platform),
			Type = type
		};

		return response;
	}

	#endregion
}