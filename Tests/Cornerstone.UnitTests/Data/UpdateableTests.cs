#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.AvaloniaEdit;
using Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;
using Cornerstone.Avalonia.DockingManager;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.EntityFramework;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Testing;
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
			typeof(Cornerstone.Avalonia.Windows.WebBrowserAdapter)
		};
		var settings = new ComparerSettings();
		settings.TypeIncludeExcludeSettings.Add(typeof(CompletionData),
			IncludeExcludeSettings.FromExclusions(nameof(CompletionData.Image))
		);
		settings.TypeIncludeExcludeSettings.Add(typeof(LineGeometry),
			IncludeExcludeSettings.FromExclusions(nameof(LineGeometry.Bounds), nameof(LineGeometry.ContourLength))
		);
		settings.TypeIncludeExcludeSettings.Add(typeof(Geometry),
			IncludeExcludeSettings.FromExclusions(nameof(Geometry.Bounds), nameof(Geometry.ContourLength))
		);
		settings.TypeIncludeExcludeSettings.Add(typeof(SplitFractions),
			IncludeExcludeSettings.FromExclusions(
				nameof(SplitFractions.DistinctCheck),
				nameof(SplitFractions.FilterCheck))
		);

		var types = assemblies
			.SelectMany(s => s.GetTypes())
			.Where(t => !exclusions.Contains(t))
			.Where(t => (t != null) && updateableType.IsAssignableFrom(t))
			//.Where(x => x == typeof(GamepadState))
			.Where(x =>
			{
				if (x.IsAbstract || x.IsInterface || x.ContainsGenericParameters)
				{
					return false;
				}

				if (x.GetConstructors().All(t => t.GetParameters().Any()))
				{
					// Ignore if the type does not have an empty constructor
					return false;
				}

				return x.GetCachedMethods()
					.Any(m => m.Name == nameof(IUpdateable.UpdateWith));
			})
			.ToArray();

		foreach (var type in types)
		{
			type.FullName.Dump();

			var includeExcludeSettings = settings.TypeIncludeExcludeSettings.TryGetValue(type, out var foundExclusions)
				? foundExclusions
				: IncludeExcludeSettings.Empty;

			RefreshCodeGeneratedUpdateWith(updateCodeGeneratedFiles, type,
				includeExcludeSettings, SolutionDirectory);

			var modelWithDefaults = GetModelWithDefaultValues(type, includeExcludeSettings);
			var modelWithNonDefault = GetModelWithNonDefaultValues(type, includeExcludeSettings);

			ValidateUnwrap(modelWithNonDefault, settings);
			ValidateAllValuesAreNotDefault(modelWithNonDefault, includeExcludeSettings);

			ValidateUpdateWith(modelWithDefaults, includeExcludeSettings, settings);
			ValidateUpdateWith(modelWithNonDefault, includeExcludeSettings, settings);
		}
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