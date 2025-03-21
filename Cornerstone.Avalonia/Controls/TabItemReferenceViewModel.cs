#region References

using System;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class TabItemReferenceViewModel
{
	#region Constructors

	public TabItemReferenceViewModel() : this(string.Empty, string.Empty, typeof(object))
	{
	}

	public TabItemReferenceViewModel(string tabName, string tabIcon, Type tabType)
	{
		TabName = tabName;
		TabIcon = tabIcon;
		TabTypeName = tabType.ToAssemblyName();
	}

	#endregion

	#region Properties

	public string TabIcon { get; set; }

	public string TabName { get; set; }

	public string TabTypeName { get; set; }

	#endregion
}