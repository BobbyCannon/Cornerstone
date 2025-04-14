#region References

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.HexEditor.Document;
using Cornerstone.Avalonia.HexEditor.Rendering;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Security.SecurityKeys;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabFileEditor : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "File Editor";

	#endregion

	#region Constructors

	public TabFileEditor()
	{
		DataContext = this;

		InitializeComponent();
	}

	#endregion
}