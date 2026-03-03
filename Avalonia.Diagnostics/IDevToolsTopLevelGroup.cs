#region References

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

#endregion

namespace Avalonia.Diagnostics;

public interface IDevToolsTopLevelGroup
{
	#region Properties

	IReadOnlyList<TopLevel> Items { get; }

	#endregion
}

public class ClassicDesktopStyleApplicationLifetimeTopLevelGroup : IDevToolsTopLevelGroup
{
	#region Fields

	private readonly IClassicDesktopStyleApplicationLifetime _lifetime;

	#endregion

	#region Constructors

	public ClassicDesktopStyleApplicationLifetimeTopLevelGroup(IClassicDesktopStyleApplicationLifetime lifetime)
	{
		_lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
	}

	#endregion

	#region Properties

	public IReadOnlyList<TopLevel> Items => _lifetime.Windows;

	#endregion

	#region Methods

	public override bool Equals(object obj)
	{
		return obj is ClassicDesktopStyleApplicationLifetimeTopLevelGroup g && (g._lifetime == _lifetime);
	}

	public override int GetHashCode()
	{
		return _lifetime.GetHashCode();
	}

	#endregion
}

public class SingleViewTopLevelGroup : IDevToolsTopLevelGroup
{
	#region Fields

	private readonly TopLevel _topLevel;

	#endregion

	#region Constructors

	public SingleViewTopLevelGroup(TopLevel topLevel)
	{
		_topLevel = topLevel;
		Items = [topLevel ?? throw new ArgumentNullException(nameof(topLevel))];
	}

	#endregion

	#region Properties

	public IReadOnlyList<TopLevel> Items { get; }

	#endregion

	#region Methods

	public override bool Equals(object obj)
	{
		return obj is SingleViewTopLevelGroup g && (g._topLevel == _topLevel);
	}

	public override int GetHashCode()
	{
		return _topLevel.GetHashCode();
	}

	#endregion
}