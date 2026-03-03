#region References

using Cornerstone.Avalonia;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabTextEditor : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Text Editor";

	#endregion

	#region Constructors

	public TabTextEditor() : this(CornerstoneApplication.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabTextEditor(IRuntimeInformation runtimeInformation)
	{
		Profiler = new Profiler();
		RuntimeInformation = runtimeInformation;
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	[RelayCommand]
	public void Load(object args)
	{
		switch (args?.ToString()?.ToLower())
		{
			case "ascii":
			{
				TextEditor.ViewModel.Document.Load(
					"""
					 ██████╗ ██████╗ ██████╗ ███╗   ██╗███████╗██████╗ ███████╗████████╗ ██████╗ ███╗   ██╗███████╗
					██╔════╝██╔═══██╗██╔══██╗████╗  ██║██╔════╝██╔══██╗██╔════╝╚══██╔══╝██╔═══██╗████╗  ██║██╔════╝
					██║     ██║   ██║██████╔╝██╔██╗ ██║█████╗  ██████╔╝███████╗   ██║   ██║   ██║██╔██╗ ██║█████╗  
					██║     ██║   ██║██╔══██╗██║╚██╗██║██╔══╝  ██╔══██╗╚════██║   ██║   ██║   ██║██║╚██╗██║██╔══╝  
					╚██████╗╚██████╔╝██║  ██║██║ ╚████║███████╗██║  ██║███████║   ██║   ╚██████╔╝██║ ╚████║███████╗
					 ╚═════╝ ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝    ╚═════╝ ╚═╝  ╚═══╝╚══════╝
					"""
				);
				break;
			}
			case "emoji":
			{
				TextEditor.ViewModel.Document.Load("💯☢️❗♒♾️⚕️😀😃😶‍🌫️🐾💪👍🫶✨🎉🎃🕯️");
				break;
			}
			case "json":
			{
				TextEditor.ViewModel.Document.Load(
					"""
					{
						"Number": 1.2345,
						"Name": "John Doe"
					}
					"""
				);

				break;
			}
			case "large":
			{
				using var rented = StringBuilderPool.Rent();
				for (var i = 0; i < 25_000; i++)
				{
					RandomGenerator.AppendString(rented.Value, RandomGenerator.NextInteger(1, 300));
					rented.Value.AppendLine();
				}
				TextEditor.ViewModel.Document.Load(rented.Value.ToString());
				break;
			}
			case "huge":
			{
				using var rented = StringBuilderPool.Rent();
				for (var i = 0; i < 100_000; i++)
				{
					RandomGenerator.AppendString(rented.Value, RandomGenerator.NextInteger(1, 300));
					rented.Value.AppendLine();
				}
				TextEditor.ViewModel.Document.Load(rented.Value.ToString());
				break;
			}
			case "text":
			{
				TextEditor.ViewModel.Document.Load(
					"""
					The quick brown fox jumped over the lazy dog.

					I'm Bobby, a software architect and chicken steward — an unusual mix, but I thrive where code meets homestead life.

					I love designing and building software. I created Cornerstone to document my personal journey of growth and learning through code. Cornerstone is a flexible, robust platform for developers to build their applications.

					Calling me passionate is an understatement. I take each day as an opportunity to learn from my previous day's failures and successes. Each day is a chance to take this information to adjust and adapt. We have a choice to grow and advance or to slip and miss the opportunities before us.

					I’m driven by studying scripture, crafting software, homesteading, and being the best husband I can be. I’m a lifelong learner, setting bold, near-impossible goals to push my limits.

					Explore my software projects, homesteading adventures, or thoughts on living with purpose to see how I continue to learn and grow.
					"""
				);
				break;
			}
			case "xml":
			{
				TextEditor.ViewModel.Document.Load(
					"""
					<Account Name="John">
						<Account Name="Johnny" />
					</Account>
					"""
				);
				break;
			}
		}
	}

	#endregion
}