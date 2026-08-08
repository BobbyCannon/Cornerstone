#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.Text.History;
using Cornerstone.Data;
using Cornerstone.Generators;
using Cornerstone.Parsers;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabTerminal : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Terminal";
	private const string AnsiCyan = "\e[96m";
	private const string AnsiGreen = "\e[92m";
	private const string AnsiReset = "\e[0m";
	private const string AnsiYellow = "\e[93m";

	#endregion

	#region Fields

	private bool _selectingDueToCaretMove;
	private readonly DispatcherTimer _timer;

	#endregion

	#region Constructors

	public TabTerminal() : this(AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabTerminal(IRuntimeInformation runtimeInformation)
	{
		_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, ProviderUpdate) { IsEnabled = false };

		Profiler = new Profiler();
		RuntimeInformation = runtimeInformation;

		(TextEditorRenderData, _) = Profiler.SetupScopeHistory(nameof(Render), 30);
		(TextEditorCommandData, _) = Profiler.SetupScopeHistory("Command", 30);

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public IRuntimeInformation RuntimeInformation { get; }

	[Notify]
	public partial Token SelectedToken { get; set; }

	public ISeriesDataProvider TextEditorCommandData { get; }

	public ISeriesDataProvider TextEditorRenderData { get; }

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		CommandChart.ValueFormatter = HumanizeTimeSpan;
		RenderChart.ValueFormatter = HumanizeTimeSpan;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			_timer.IsEnabled = true;
		}

		base.OnAttachedToVisualTree(e);

		Terminal.ViewModel.Caret.CaretMoved += CaretOnCaretMoved;
		Terminal.CommandEntered += TerminalOnCommandEntered;

		if (Design.IsDesignMode)
		{
			Terminal.ViewModel.CommandHistoryProvider.Add(new CommandHistory("help"));
			Terminal.ViewModel.CommandHistoryProvider.Add(new CommandHistory("sleep 2"));
			Terminal.ViewModel.CommandHistoryProvider.Add(new CommandHistory("error demo"));
			Terminal.ViewModel.CommandHistoryProvider.Add(new CommandHistory("clear"));
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_timer.IsEnabled = false;
		Terminal.ViewModel.Caret.CaretMoved -= CaretOnCaretMoved;
		Terminal.CommandEntered -= TerminalOnCommandEntered;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(string propertyName)
	{
		if (propertyName == nameof(SelectedToken))
		{
			var token = SelectedToken;
			if ((SelectedToken != null) && !_selectingDueToCaretMove)
			{
				Terminal.ViewModel.Caret.Selection.Update(token.StartOffset, token.EndOffset);

				if (Terminal.ViewModel.Lines.TryGetLineForOffset(token.StartOffset, out var line))
				{
					Terminal.ScrollToLine(line.LineNumber);
				}
			}
		}
		base.OnPropertyChanged(propertyName);
	}

	private void CaretOnCaretMoved(object sender, EventArgs e)
	{
		var vm = Terminal.ViewModel;
		var token = vm.TokenManager.FirstOrDefault(x => x.Contains(vm.Caret.Offset));
		if (token == null)
		{
			return;
		}
		_selectingDueToCaretMove = true;
		TokenItems.SelectedItem = token;
		_selectingDueToCaretMove = false;
	}

	private void ClearCommandHistory(object sender, RoutedEventArgs e)
	{
		Terminal.ViewModel.CommandHistoryProvider.Clear();
	}

	private void ClearTerminal(object sender, RoutedEventArgs e)
	{
		Reset();
		Terminal.Focus(NavigationMethod.Pointer);
	}

	private static string HumanizeTimeSpan(double value)
	{
		return TimeSpan.FromTicks((long) value).Humanize();
	}

	private void PrintAnsi256Palette()
	{
		const string colorLabel = "Foreground";
		const string bgLabel = "Background";

		// Palette demos intentionally use AppendText for raw SGR construction.
		Terminal.AppendText($"{AnsiYellow}ANSI 256 Color Palette:{AnsiReset}\r\n\r\n");

		// Section 1: Standard + High Intensity (0-15)
		Terminal.AppendText($"{AnsiCyan}Standard and High Intensity ({colorLabel}):{AnsiReset}\r\n");
		for (var i = 0; i < 16; i++)
		{
			Terminal.AppendText($"\e[38;5;{i}m{i,-3} ");
			if (((i + 1) % 4) == 0)
			{
				Terminal.AppendText("\r\n");
			}
		}

		// Section 2: Color Cube (16-231)
		Terminal.AppendText($"\r\n{AnsiCyan}Color Cube ({colorLabel}) - 6x6x6 (16-231):{AnsiReset}\r\n");
		for (var i = 16; i <= 231; i++)
		{
			Terminal.AppendText($"\e[38;5;{i}m{i,-3} ");
			if ((((i - 16) + 1) % 36) == 0)
			{
				Terminal.AppendText("\r\n");
			}
		}

		// Section 3: Grayscale Ramp (232-255)
		Terminal.AppendText($"\r\n{AnsiCyan}Grayscale Ramp ({colorLabel}) (232-255):{AnsiReset}\r\n");
		for (var i = 232; i <= 255; i++)
		{
			Terminal.AppendText($"\e[38;5;{i}m{i,-3} ");
			if ((((i - 232) + 1) % 12) == 0)
			{
				Terminal.AppendText("\r\n");
			}
		}

		Terminal.AppendText($"\r\n\r\n{AnsiYellow}ANSI 256 Color Palette ({bgLabel}):{AnsiReset}\r\n\r\n");

		// Background section: Standard + High Intensity (0-15)
		Terminal.AppendText($"{AnsiCyan}Standard and High Intensity ({bgLabel}):{AnsiReset}\r\n");
		for (var i = 0; i < 16; i++)
		{
			Terminal.AppendText($"\e[48;5;{i}m{i,-3} ");
			if (((i + 1) % 4) == 0)
			{
				Terminal.AppendText("\r\n");
			}
		}

		// Background section: Color Cube (16-231)
		Terminal.AppendText($"\r\n{AnsiCyan}Color Cube ({bgLabel}) - 6x6x6 (16-231):{AnsiReset}\r\n");
		for (var i = 16; i <= 231; i++)
		{
			Terminal.AppendText($"\e[48;5;{i}m{i,-3} ");
			if ((((i - 16) + 1) % 36) == 0)
			{
				Terminal.AppendText("\r\n");
			}
		}

		// Background section: Grayscale Ramp (232-255)
		Terminal.AppendText($"\r\n{AnsiCyan}Grayscale Ramp ({bgLabel}) (232-255):{AnsiReset}\r\n");
		for (var i = 232; i <= 255; i++)
		{
			Terminal.AppendText($"\e[48;5;{i}m{i,-3} ");
			if ((((i - 232) + 1) % 12) == 0)
			{
				Terminal.AppendText("\r\n");
			}
		}

		Terminal.AppendText(AnsiReset);
	}

	private void PrintAnsiTextFormatting()
	{
		Terminal.AppendText($"{AnsiYellow}ANSI Text Formatting Codes:{AnsiReset}\r\n\r\n");

		var samples = new[]
		{
			("\e[1m", "Bold", "\e[1mBold text\e[0m"),
			("\e[2m", "Dim", "\e[2mDim text\e[0m"),
			("\e[3m", "Italic", "\e[3mItalic text\e[0m"),
			("\e[4m", "Underline", "\e[4mUnderlined text\e[0m"),
			("\e[5m", "Blink", "\e[5mBlinking text\e[0m"),
			("\e[7m", "Reverse Video", "\e[7mReversed text\e[0m"),
			("\e[8m", "Concealed", "\e[8mConcealed text\e[0m"),
			("\e[9m", "Strikethrough", "\e[9mStrikethrough text\e[0m")
		};

		foreach (var (_, label, formatted) in samples)
		{
			Terminal.AppendText($"{AnsiCyan}{label,-18}{AnsiReset}    {formatted}\r\n");
		}

		Terminal.AppendText($"\r\n{AnsiYellow}Combined Formatting:{AnsiReset}\r\n\r\n");

		var combined = new[]
		{
			("\e[1;3m", "Bold + Italic"),
			("\e[1;4m", "Bold + Underline"),
			("\e[3;9m", "Italic + Strikethrough"),
			("\e[1;3;4m", "Bold + Italic + Underline"),
			("\e[2;3m", "Dim + Italic"),
			("\e[4;9m", "Underline + Strikethrough")
		};

		foreach (var (code, label) in combined)
		{
			Terminal.AppendText($"{AnsiCyan}{label,-25}{AnsiReset}    {code}Sample text{AnsiReset}\r\n");
		}

		Terminal.AppendText(AnsiReset);
	}

	private void PrintColorCombinations(Range fgRange, Range bgRange)
	{
		var fgCodes = Enumerable.Range(fgRange.Start.Value, fgRange.End.Value - fgRange.Start.Value).ToArray();
		var bgCodes = Enumerable.Range(bgRange.Start.Value, bgRange.End.Value - bgRange.Start.Value).ToArray();

		var column = 0;
		const int maxColumns = 8;

		foreach (var fg in fgCodes)
		{
			foreach (var bg in bgCodes)
			{
				Terminal.AppendText($"\e[{bg}m\e[{fg}m foo bar ");

				if (++column >= maxColumns)
				{
					Terminal.AppendText("\r\n");
					column = 0;
				}
			}
		}

		if (column > 0)
		{
			Terminal.AppendText("\r\n");
		}
	}

	/// <summary>
	/// Sample host command processor. Prefer <see cref="Cornerstone.Avalonia.Text.Terminal.WriteOutput" /> /
	/// <see cref="Cornerstone.Avalonia.Text.Terminal.WriteError" /> for host text; use AppendText for raw ANSI demos.
	/// </summary>
	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	private async Task ProcessCommandAsync(string command)
	{
		var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return;
		}

		var firstWord = parts[0].ToLowerInvariant();
		var argument = parts.Length > 1 ? string.Join(" ", parts[1..]) : null;

		switch (firstWord)
		{
			case "help":
			{
				Terminal.WriteOutput($"{AnsiYellow}Available commands:{AnsiReset}\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}clear{AnsiReset}       - Clear the terminal\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}echo{AnsiReset}        - Print text (WriteOutput)\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}error{AnsiReset}       - Print an error (WriteError)\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}date{AnsiReset}        - Show current date/time\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}sleep [n]{AnsiReset}   - Wait n seconds (default 2); shows input lock\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}ansibasic{AnsiReset}   - ANSI basic color palette\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}ansi256{AnsiReset}     - ANSI 256 color palette\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}ansiformat{AnsiReset}  - ANSI text formatting codes\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}console{AnsiReset}     - Console color palette\r\n");
				Terminal.WriteOutput($"    {AnsiCyan}flood{AnsiReset}       - Inject a large amount of text\r\n");
				Terminal.WriteOutput("\r\n");
				Terminal.WriteOutput($"{AnsiYellow}Host notes:{AnsiReset}\r\n");
				Terminal.WriteOutput("    CommandEntered → WriteOutput/WriteError → EndCommand()\r\n");
				Terminal.WriteOutput("    While busy, typing/history/paste are locked; scroll still works.\r\n");
				Terminal.WriteOutput("    Prompt text is read-only; Home stops after the prompt.\r\n");
				break;
			}
			case "clear":
			case "cls":
			{
				// Reset already re-prompts; EndCommand in finally will no-op the duplicate prompt.
				Reset();
				break;
			}
			case "echo":
			{
				Terminal.WriteOutput($"{argument ?? string.Empty}\r\n");
				break;
			}
			case "error":
			{
				Terminal.WriteError($"{argument ?? "Sample error from WriteError"}\r\n");
				break;
			}
			case "date":
			{
				Terminal.WriteOutput($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\r\n");
				break;
			}
			case "sleep":
			case "delay":
			{
				var seconds = 2;
				if (int.TryParse(argument, out var parsed) && (parsed > 0))
				{
					seconds = Math.Min(parsed, 30);
				}

				Terminal.WriteOutput($"Sleeping {seconds}s — watch Host status and try typing...\r\n");
				await Task.Delay(TimeSpan.FromSeconds(seconds));
				Terminal.WriteOutput("Done.\r\n");
				break;
			}
			case "ansibasic":
			case "ansibasiccolors":
			{
				PrintColorCombinations(30..38, 40..48);
				PrintColorCombinations(90..98, 40..48);
				PrintColorCombinations(30..38, 100..108);
				PrintColorCombinations(90..98, 100..108);
				break;
			}
			case "ansi256":
			case "ansi256colors":
			{
				PrintAnsi256Palette();
				break;
			}
			case "ansiformats":
			{
				PrintAnsiTextFormatting();
				break;
			}
			case "console":
			case "consolecolors":
			{
				var i = 0;
				foreach (var bg in ColorExtensions.ConsoleColorMap)
				{
					foreach (var fg in ColorExtensions.ConsoleColorMap)
					{
						// Colored overload demo (not ANSI escape text).
						Terminal.AppendText(" foo bar ", fg.Value, bg.Value);

						if (i++ >= 7)
						{
							Terminal.WriteOutput("\r\n");
							i = 0;
						}
					}
				}
				break;
			}
			case "flood":
			{
				for (var i = 0; i < 100; i++)
				{
					Terminal.WriteOutput($"Line {i}: Simulating console output data stream...\r\n");
				}
				break;
			}
			default:
			{
				Terminal.WriteError($"Unknown command: {firstWord}\r\n");
				break;
			}
		}
	}

	private void ProviderUpdate(object sender, EventArgs e)
	{
		Profiler.Refresh();
	}

	private void Reset()
	{
		Terminal.Clear();
		Terminal.WriteOutput($"{AnsiGreen}Cornerstone Terminal v1.1{AnsiReset}\r\n");
		Terminal.WriteOutput($"{AnsiCyan}Type 'help' for commands. Try 'sleep 2' to see the processing lock.{AnsiReset}\r\n");
		Terminal.PromptForCommand();
		Terminal.Focus();
	}

	private void TerminalCommand(object sender, RoutedEventArgs e)
	{
		var button = (Button) sender;
		var label = button.Content?.ToString() ?? string.Empty;
		// Map button labels to typed commands (spaces removed for most; Sleep/Error need args).
		var command = label switch
		{
			"Sleep" => "sleep 2",
			"Error" => "error demo from button",
			_ => label.Replace(" ", string.Empty)
		};

		Terminal.ExecuteCommand(command);
		Terminal.Focus(NavigationMethod.Pointer);
	}

	private async void TerminalOnCommandEntered(object sender, string command)
	{
		// TimedScope is a ref struct and cannot span await; record duration manually.
		var profiler = Profiler;
		var startTicks = profiler?.GetTicks() ?? 0;
		try
		{
			await ProcessCommandAsync(command);
		}
		finally
		{
			if (profiler != null)
			{
				var elapsed = profiler.GetTicks() - startTicks;
				profiler.OnScopeEnded(new TimedScope("Command", profiler), elapsed);
			}

			// Clears IsCommandProcessing and shows the next prompt.
			Terminal.EndCommand();
		}
	}

	private void TerminalOnLoaded(object sender, RoutedEventArgs e)
	{
		Reset();
	}

	private void TerminalScrollTo(object sender, RoutedEventArgs e)
	{
		var button = (Button) sender;
		var content = button.Content?.ToString()?.ToLower() ?? string.Empty;
		if (content.Contains("top"))
		{
			Terminal.ScrollToHome();
		}
		else if (content.Contains("bottom"))
		{
			Terminal.ScrollToEnd();
		}
		else
		{
			var line = RandomGenerator.NextInteger(1, Terminal.ViewModel.Lines.Count);
			Terminal.ScrollToLine(line);
		}
		Terminal.Focus(NavigationMethod.Pointer);
	}

	#endregion
}
