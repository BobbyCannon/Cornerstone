#region References

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabUpdateable : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Updateable";

	#endregion

	#region Constructors

	public TabUpdateable()
	{
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public bool IsRunning { get; private set; }

	#endregion

	#region Methods

	public static string TestUpdates(int count)
	{
		var sourceItems = new TestNotifiable[count];
		var updateWithDirectItems = new TestNotifiable[count];
		var updateWithItems = new TestNotifiable[count];
		var settings = new IncludeExcludeSettings();
		var setupProfiler = new Profiler("Setup");
		var profiler = new Profiler("Updateable");
		var details = EnumExtensions.GetAllEnumDetails<UpdateableAction>();
		var reflectionItems = new Dictionary<UpdateableAction, TestNotifiable[]>();

		setupProfiler.Time("SourceItems",
			() =>
			{
				foreach (var detail in details)
				{
					reflectionItems.Add(detail.Key, new TestNotifiable[count]);
				}

				for (var index = 0; index < sourceItems.Length; index++)
				{
					sourceItems[index] = new TestNotifiable();
					sourceItems[index].UpdateWithNonDefaultValues(settings);

					updateWithDirectItems[index] = new TestNotifiable();
					updateWithItems[index] = new TestNotifiable();

					foreach (var detail in details)
					{
						reflectionItems[detail.Key][index] = new TestNotifiable();
					}
				}
			});

		profiler.Time("UpdateWithDirect",
			() =>
			{
				for (var index = 0; index < sourceItems.Length; index++)
				{
					updateWithDirectItems[index].UpdateWithDirect(sourceItems[index]);
				}
			});

		profiler.Time("UpdateWithSettingCheck",
			() =>
			{
				for (var index = 0; index < sourceItems.Length; index++)
				{
					updateWithDirectItems[index].UpdateWithSettingCheck(sourceItems[index], settings);
				}
			});

		profiler.Time("UpdateWith",
			() =>
			{
				for (var index = 0; index < sourceItems.Length; index++)
				{
					updateWithItems[index].UpdateWith(sourceItems[index]);
				}
			});

		foreach (var item in reflectionItems)
		{
			profiler.Time($"UpdateWithUsingReflection for {item.Key}",
				() =>
				{
					for (var index = 0; index < sourceItems.Length; index++)
					{
						item.Value[index].UpdateWithUsingReflection(sourceItems[index], UpdateableAction.Updateable);
					}
				});
		}

		var builder = new TextBuilder();
		builder.AppendLine(setupProfiler.ToString());
		builder.AppendLine(profiler.ToString());
		return builder.ToString();
	}

	private void TestOnClick(object sender, RoutedEventArgs e)
	{
		IsRunning = true;

		Task.Run(() =>
		{
			var result = TestUpdates(10000);

			this.Dispatch(() =>
			{
				Results.Text = result;
				IsRunning = false;
			});
		});
	}

	#endregion

	#region Interfaces

	public interface ITestData
	{
		#region Properties

		bool IsEnabled { get; set; }
		Location.Location Location { get; set; }
		string Name { get; set; }
		decimal Payment { get; set; }
		double Percent { get; set; }

		#endregion
	}

	#endregion

	#region Classes

	public class TestNotifiable : Notifiable<TestNotifiable>, ITestData
	{
		#region Properties

		[SyncProperty(UpdateableAction.SyncIncomingUpdate | UpdateableAction.SyncOutgoing)]
		public bool IsEnabled { get; set; }

		public Location.Location Location { get; set; }

		[SyncProperty(UpdateableAction.SyncAll)]
		public string Name { get; set; }

		public decimal Payment { get; set; }

		public double Percent { get; set; }

		#endregion

		#region Methods

		public override bool UpdateWith(TestNotifiable update, IncludeExcludeSettings settings)
		{
			UpdateProperty(IsEnabled, update.IsEnabled, settings.ShouldProcessProperty(nameof(IsEnabled)), x => IsEnabled = x);
			UpdateProperty(Location, update.Location, settings.ShouldProcessProperty(nameof(Location)), x => Location = x);
			UpdateProperty(Name, update.Name, settings.ShouldProcessProperty(nameof(Name)), x => Name = x);
			return true;
		}

		public override bool UpdateWith(object update, IncludeExcludeSettings settings)
		{
			return update switch
			{
				TestNotifiable sValue => UpdateWith(sValue, settings),
				_ => base.UpdateWith(update, settings)
			};
		}

		public bool UpdateWithDirect(TestNotifiable update)
		{
			IsEnabled = update.IsEnabled;
			Location = update.Location;
			Name = update.Name;
			return true;
		}

		public bool UpdateWithSettingCheck(TestNotifiable update, IncludeExcludeSettings settings)
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsEnabled)), x => x.IsEnabled = update.IsEnabled);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Location)), x => x.Location = update.Location);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Name)), x => x.Name = update.Name);
			return true;
		}

		#endregion
	}

	#endregion
}