#region References

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.DockingManager;
using Cornerstone.Avalonia.Platforms.Windows;
using Cornerstone.Avalonia.TextEditor;
using Cornerstone.Avalonia.TextEditor.CodeCompletion;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.EntityFramework;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class UpdateableTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ReflectionCloneable()
	{
		var details = new ProcessDetails
		{
			Arguments = "--Version",
			FilePath = "git.exe",
			ProcessId = 12345,
			StartedOn = new DateTime(2024, 05, 15, 08, 45, 00, 00, DateTimeKind.Utc),
			StoppedOn = new DateTime(2024, 05, 15, 08, 45, 00, 123, DateTimeKind.Utc),
			WorkingDirectory = "C:\\Workspaces",
			ExitCode = 0,
			WasCancelled = false
		};

		AreEqual(TimeSpan.FromMilliseconds(123), details.Duration);

		var actual = details.ShallowClone();

		AreEqual(details, actual);
	}

	[TestMethod]
	public void UpdateableShouldUpdateAll()
	{
		var updateCodeGeneratedFiles = EnableFileUpdates || IsDebugging;
		var updateableType = typeof(IUpdateable);
		var assemblies = new[]
		{
			// Cornerstone
			typeof(Database).Assembly,
			// Cornerstone.Avalonia
			typeof(CornerstoneApplication).Assembly,
			// Cornerstone.EntityFramework
			typeof(EntityFrameworkDatabase).Assembly
		};
		var exclusions = new List<Type>
		{
			typeof(WebBrowserAdapter)
		};
		var settings = new ComparerSettings { IgnoreMissingProperties = true };
		settings.TypeIncludeExcludeSettings.Add(typeof(CompletionData),
			new[] { nameof(CompletionData.Image) }.ToOnlyExcludingSettings()
		);
		settings.TypeIncludeExcludeSettings.Add(typeof(LineGeometry),
			new[] { nameof(LineGeometry.Bounds), nameof(LineGeometry.ContourLength) }.ToOnlyExcludingSettings()
		);
		settings.TypeIncludeExcludeSettings.Add(typeof(Geometry),
			new[] { nameof(Geometry.Bounds), nameof(Geometry.ContourLength) }.ToOnlyExcludingSettings()
		);
		settings.TypeIncludeExcludeSettings.Add(typeof(ContextMenu),
			new[] { nameof(ContextMenu.Bounds), nameof(ContextMenu.FontFamily) }.ToOnlyExcludingSettings()
		);
		settings.TypeIncludeExcludeSettings.Add(typeof(MenuItemData),
			new[]
			{
				nameof(MenuItemData.FilterCheck)
			}.ToOnlyExcludingSettings()
		);
		settings.TypeIncludeExcludeSettings.Add(typeof(SplitFractions),
			new[]
			{
				nameof(SplitFractions.DistinctCheck),
				nameof(SplitFractions.FilterCheck)
			}.ToOnlyExcludingSettings()
		);

		UpdateableShouldUpdateAll(
			updateCodeGeneratedFiles, updateableType, assemblies, exclusions, settings
			//, x => x == typeof(PopupManager)
		);
	}

	/// <inheritdoc />
	protected override object CreateCustomTypeFactory(Type type, object[] args)
	{
		if (type == typeof(TextEditorSettings))
		{
			var response = (TextEditorSettings) CreateInstanceOfNonDefaultValue(type);
			return response;
		}
		if (type == typeof(Timer))
		{
			var response = new Timer(this);
			return response;
		}
		return base.CreateCustomTypeFactory(type, args);
	}

	#endregion
}