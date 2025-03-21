#region References

using System.Runtime.CompilerServices;
using Cornerstone.Attributes;
using Cornerstone.Collections;
using Cornerstone.Data;

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

	/// <summary>
	/// Update the TargetFramework with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(TargetFramework update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith - TargetFramework

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(IsNetFramework, update.IsNetFramework, settings.ShouldProcessProperty(nameof(IsNetFramework)), x => IsNetFramework = x);
		UpdateProperty(IsNetStandard, update.IsNetStandard, settings.ShouldProcessProperty(nameof(IsNetStandard)), x => IsNetStandard = x);
		UpdateProperty(IsUniversalFramework, update.IsUniversalFramework, settings.ShouldProcessProperty(nameof(IsUniversalFramework)), x => IsUniversalFramework = x);
		UpdateProperty(NetStandard, update.NetStandard, settings.ShouldProcessProperty(nameof(NetStandard)), x => NetStandard = x);
		UpdateProperty(Platform, update.Platform, settings.ShouldProcessProperty(nameof(Platform)), x => Platform = x);

		// Code Generated - /UpdateWith - TargetFramework

		return base.UpdateWith(update, settings);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			TargetFramework value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(Parent):
			{
				Parent.Children ??= [];
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