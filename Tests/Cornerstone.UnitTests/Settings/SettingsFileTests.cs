#region References

using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Settings;

[TestClass]
public class SettingsFileTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Load()
	{
		var settings = new SampleSettings(TempDirectory);
		settings.Load();
		IsTrue(settings.IsLoaded);
		IsFalse(settings.HasChanges());

		settings.IsEnabled = true;
		settings.Options.Add("abc", "123");
		IsTrue(settings.HasChanges());
		
		var json = settings.ToJson();
		AreEqual("{\"IsEnabled\":true,\"Options\":[\"abc\",\"123\"]}", json);

		settings.Reset();
		IsFalse(settings.IsEnabled);
		IsFalse(settings.IsLoaded);
		AreEqual(0, settings.Options.Count);

		settings.Load(json);
		IsTrue(settings.IsEnabled);
		IsTrue(settings.IsLoaded);
		IsFalse(settings.HasChanges());
		AreEqual(2, settings.Options.Count);
		AreEqual("abc", settings.Options[0]);
		AreEqual("123", settings.Options[1]);
	}

	#endregion

	#region Classes

	internal class SampleSettings : SettingsFile<SampleSettings>
	{
		#region Constructors

		/// <summary>
		/// For serialization, do not use.
		/// </summary>
		public SampleSettings() : this(null)
		{
		}

		/// <inheritdoc />
		public SampleSettings(string directory) : base("sample.json", directory, null)
		{
			Options = new SpeedyList<string>(GetDispatcher());
		}

		#endregion

		#region Properties

		public bool IsEnabled
		{
			get => Get(x => x.IsEnabled, false);
			set => Set(x => x.IsEnabled, value);
		}

		public IList<string> Options { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Update the SampleSettings with an update.
		/// </summary>
		/// <param name="update"> The update to be applied. </param>
		public virtual bool UpdateWith(SampleSettings update)
		{
			return UpdateWith(update, IncludeExcludeSettings.Empty);
		}

		/// <summary>
		/// Update the SampleSettings with an update.
		/// </summary>
		/// <param name="update"> The update to be applied. </param>
		/// <param name="settings> The options for controlling the updating of the entity. </param>
		public virtual bool UpdateWith(SampleSettings update, IncludeExcludeSettings settings)
		{
			// If the update is null then there is nothing to do.
			if (update == null)
			{
				return false;
			}

			// ****** You can use GenerateUpdateWith to update this ******

			if ((settings == null) || settings.IsEmpty())
			{
				IsEnabled = update.IsEnabled;
				Options.Reconcile(update.Options);
			}
			else
			{
				this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsEnabled)), x => x.IsEnabled = update.IsEnabled);
				this.IfThen(_ => settings.ShouldProcessProperty(nameof(Options)), x => x.Options.Reconcile(update.Options));
			}

			return true;
		}

		/// <inheritdoc />
		public override bool UpdateWith(object update, IncludeExcludeSettings settings)
		{
			return update switch
			{
				SampleSettings value => UpdateWith(value, settings),
				_ => base.UpdateWith(update, settings)
			};
		}

		/// <inheritdoc />
		protected override void ResetToDefaults()
		{
			IsEnabled = false;
			Options.Clear();
			base.ResetToDefaults();
		}

		#endregion
	}

	#endregion
}