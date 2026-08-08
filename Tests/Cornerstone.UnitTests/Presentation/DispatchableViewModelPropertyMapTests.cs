#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
[SourceReflection]
public partial class DispatchableViewModelPropertyMapTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void MapOneWayDoesNotWriteModel()
	{
		var model = new SettingsModel { Path = "a" };
		model.ResetHasChanges();
		var vm = new AgentHostViewModel(model);
		vm.RegisterOneWayPath();

		vm.ApplyModelChanges(); // seed
		AreEqual("a", vm.SelectedPath);

		vm.SelectedPath = "user-edit";
		// One-way maps do not treat view dirty bits as pending work
		IsFalse(vm.HasModelChanges());
		vm.ApplyModelChanges();

		// One-way: view change is not written to model
		AreEqual("a", model.Path);
		AreEqual("user-edit", vm.SelectedPath);
	}

	[TestMethod]
	public void MapTwoWayDifferentNames()
	{
		var model = new SettingsModel { FavoriteModel = @"C:\models\x.gguf" };
		model.ResetHasChanges();
		var vm = new AgentHostViewModel(model);
		vm.RegisterTwoWayFavoriteToSelectedPath();

		// Seed
		IsTrue(vm.HasModelChanges());
		vm.ApplyModelChanges();
		AreEqual(@"C:\models\x.gguf", vm.SelectedPath);

		// Model → view
		model.FavoriteModel = @"C:\models\y.gguf";
		IsTrue(vm.HasModelChanges());
		vm.ApplyModelChanges();
		AreEqual(@"C:\models\y.gguf", vm.SelectedPath);

		// View → model
		vm.SelectedPath = @"C:\models\z.gguf";
		IsTrue(vm.HasModelChanges());
		vm.ApplyModelChanges();
		AreEqual(@"C:\models\z.gguf", model.FavoriteModel);
	}

	[TestMethod]
	public void MapTwoWayDifferentTypes()
	{
		var model = new SettingsModel { Path = @"C:\models\qwen.gguf" };
		model.ResetHasChanges();
		var vm = new AgentHostViewModel(model);
		vm.RegisterTwoWayPathToSelected();

		vm.ApplyModelChanges();
		IsNotNull(vm.Selected);
		AreEqual(@"C:\models\qwen.gguf", vm.Selected.Path);

		// Model → view
		model.Path = @"C:\models\other.gguf";
		vm.ApplyModelChanges();
		AreEqual(@"C:\models\other.gguf", vm.Selected.Path);

		// View → model
		vm.Selected = new ModelRef { Path = @"C:\models\third.gguf" };
		vm.ApplyModelChanges();
		AreEqual(@"C:\models\third.gguf", model.Path);
	}

	[TestMethod]
	public void MapTwoWayNoInfiniteLoop()
	{
		var model = new SettingsModel { Path = "stable" };
		model.ResetHasChanges();
		var vm = new AgentHostViewModel(model);
		vm.RegisterTwoWaySameName();

		vm.ApplyModelChanges();
		IsFalse(vm.HasModelChanges());
		IsFalse(model.HasChanges());

		// Second apply with no edits should stay clean
		vm.ApplyModelChanges();
		IsFalse(vm.HasModelChanges());
		IsFalse(model.HasChanges());

		model.Path = "stable"; // same value still marks change on CornerstoneObject
		vm.ApplyModelChanges();
		// After apply, bits for Path should be cleared even if value equal
		IsFalse(model.HasChanges(new[] { nameof(SettingsModel.Path) }.ToOnlyIncludingSettings()));
	}

	[TestMethod]
	public void MapTwoWaySameNameSameType()
	{
		var model = new SettingsModel { Path = "from-model" };
		model.ResetHasChanges();
		var vm = new AgentHostViewModel(model);
		vm.RegisterTwoWaySameName();

		vm.ApplyModelChanges();
		AreEqual("from-model", vm.Path);

		vm.Path = "from-view";
		vm.ApplyModelChanges();
		AreEqual("from-view", model.Path);
	}

	[TestMethod]
	public void SeedAppliesWithoutPriorChangeBits()
	{
		var model = new SettingsModel { Path = "preloaded" };
		model.ResetHasChanges(); // no dirty bits
		var vm = new AgentHostViewModel(model);
		vm.RegisterTwoWaySameName();

		IsTrue(vm.HasModelChanges()); // seed pending
		vm.ApplyModelChanges();
		AreEqual("preloaded", vm.Path);
		IsFalse(vm.HasModelChanges());
	}

	[TestMethod]
	public void UnmappedModelChangeDoesNotForceMapPendingAfterSeed()
	{
		var model = new SettingsModel { Path = "p", Notes = "n" };
		model.ResetHasChanges();
		var vm = new AgentHostViewModel(model);
		vm.RegisterTwoWaySameName(); // only Path

		vm.ApplyModelChanges(); // seed Path
		IsFalse(vm.HasModelChanges());

		model.Notes = "changed notes only";
		// Map should not report pending for unmapped Notes
		IsFalse(vm.HasModelChanges());
		AreEqual("changed notes only", model.Notes);

		// Notes bit remains on model; Path bit is clear
		IsTrue(model.HasChanges(new[] { nameof(SettingsModel.Notes) }.ToOnlyIncludingSettings()));
		IsFalse(model.HasChanges(new[] { nameof(SettingsModel.Path) }.ToOnlyIncludingSettings()));
	}

	#endregion

	#region Classes

	/// <summary>
	/// Stand-in for ModelInfo-like view value (path only).
	/// </summary>
	public sealed class ModelRef
	{
		#region Properties

		public string Path { get; set; }

		#endregion
	}

	[SourceReflection]
	[Notifiable(["*"])]
	public partial class SettingsModel : CornerstoneObject
	{
		#region Properties

		public partial string FavoriteModel { get; set; }
		public partial string Notes { get; set; }
		public partial string Path { get; set; }

		#endregion
	}

	[SourceReflection]
	[Notifiable(["*"])]
	public partial class AgentHostViewModel : DispatchableViewModel
	{
		#region Fields

		private readonly SettingsModel _settings;

		#endregion

		#region Constructors

		public AgentHostViewModel(SettingsModel settings)
		{
			_settings = settings;
		}

		#endregion

		#region Properties

		public partial string Path { get; set; }
		public partial ModelRef Selected { get; set; }
		public partial string SelectedPath { get; set; }

		#endregion

		#region Methods

		public void RegisterOneWayPath()
		{
			TrackProperties(_settings)
				.MapOneWay(nameof(SettingsModel.Path), nameof(SelectedPath), (string p) => p);
		}

		public void RegisterTwoWayFavoriteToSelectedPath()
		{
			TrackProperties(_settings)
				.MapTwoWay(nameof(SettingsModel.FavoriteModel), nameof(SelectedPath));
		}

		public void RegisterTwoWayPathToSelected()
		{
			TrackProperties(_settings)
				.MapTwoWay(
					nameof(SettingsModel.Path),
					nameof(Selected),
					(string path) => path == null ? null : new ModelRef { Path = path },
					(ModelRef selected) => selected?.Path);
		}

		public void RegisterTwoWaySameName()
		{
			TrackProperties(_settings).MapTwoWay(nameof(Path));
		}

		#endregion
	}

	#endregion
}
