#region References

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Text;
using Cornerstone.Data;
using Cornerstone.Generators;
using Cornerstone.Parsers;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;
using DispatcherPriority = Avalonia.Threading.DispatcherPriority;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabTextEditor : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Text Editor";

	#endregion

	#region Fields

	private readonly BackgroundWorker _benchmark;
	private bool _selectingDueToCaretMove;
	private readonly Profiler _textEditorRendererProfiler;
	private readonly DispatcherTimer _timer;

	#endregion

	#region Constructors

	public TabTextEditor() : this(CornerstoneApplication.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabTextEditor(IRuntimeInformation runtimeInformation)
	{
		_benchmark = new BackgroundWorker();
		_benchmark.WorkerSupportsCancellation = true;
		_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, ProviderUpdate) { IsEnabled = false };
		_textEditorRendererProfiler = new Profiler();

		(RandomGeneratedData, _) = _textEditorRendererProfiler.SetupScopeHistory("RandomGenerated", 30);
		(LineManagerRebuildData, _) = _textEditorRendererProfiler.SetupScopeHistory("LineManager.Rebuild", 30);
		(TokenManagerRebuildData, _) = _textEditorRendererProfiler.SetupScopeHistory("TokenManager.Rebuild", 30);
		(TextEditorMeasureData, _) = _textEditorRendererProfiler.SetupScopeHistory(nameof(MeasureOverride), 30);
		(TextEditorRenderData, _) = _textEditorRendererProfiler.SetupScopeHistory(nameof(Render), 30);
		(TextEditorDocumentChangedData, _) = _textEditorRendererProfiler.SetupScopeHistory(nameof(TextEditorViewModel.DocumentChanged), 30);

		RuntimeInformation = runtimeInformation;

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool IsBenchmarking { get; set; }

	public ISeriesDataProvider LineManagerRebuildData { get; }

	public ISeriesDataProvider RandomGeneratedData { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	[Notify]
	public partial Token SelectedToken { get; set; }

	public ISeriesDataProvider TextEditorDocumentChangedData { get; }

	public ISeriesDataProvider TextEditorMeasureData { get; }

	public ISeriesDataProvider TextEditorRenderData { get; }

	public ISeriesDataProvider TokenManagerRebuildData { get; }

	#endregion

	#region Methods

	[RelayCommand]
	public void Load(object args)
	{
		TextEditor.ViewModel.ConfigureForFileType(string.Empty);

		switch (args?.ToString()?.ToLower())
		{
			case "ascii":
			{
				TextEditor.ViewModel.Load(
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
			case "clear":
			{
				TextEditor.ViewModel.Load(string.Empty);
				break;
			}
			case "csharp":
			{
				TextEditor.ViewModel.ConfigureForFileType("cs");
				TextEditor.ViewModel.Load(
					"""
					#region References

					using System;
					using Cornerstone.Data;
					using Cornerstone.Reflection;
					using Cornerstone.Serialization;
					using Cornerstone.Sync;

					#endregion

					namespace Cornerstone.Sample.Models;

					/// <summary>
					/// This is a sample sync model for account.
					/// </summary>
					[SourceReflection]
					public partial class Account : SyncModel
					{
						#region Properties

						[Pack(1, 5)]
						[UpdateableAction(UpdateableAction.All)]
						public string Name { get; set; }
						
						#endregion
						
						#region Methods
						
						public void Process()
						{
							// This is a comment for process.
						}
						
						#endregion
					}
					"""
				);
				break;
			}
			case "emoji":
			{
				TextEditor.ViewModel.Load("💯☢️❗♒♾️⚕️😀😃😶‍🌫️🐾💪👍🫶✨🎉🎃🕯️");
				break;
			}
			case "json":
			{
				TextEditor.ViewModel.ConfigureForFileType("json");
				TextEditor.ViewModel.Load(
					"""
					{
						"nullValue": null,
						"errorValue": invalid,
						"booleanTrue": true,
						"booleanFalse": false,
						"integer": 42,
						"negativeInteger": -123,
						"largeInteger": 9007199254740991,
						"float": 3.14159,
						"negativeFloat": -0.000001,
						"scientificNotation": 6.02214076e+23,
						"zero": 0,
						"negativeZero": -0,
						"stringEmpty": "",
						"stringNormal": "Hello, world!",
						"stringWithEscapes": "Line1\nLine2\tTabbed\rCarriage Return",
						"stringWithUnicode": "Unicode: café, 世界, 😀, \uD83D\uDE00",
						"stringWithQuotes": "He said: \"Hello World\"",
						"stringWithBackslash": "Path: C:\\Users\\Documents",
						"stringWithControlChars": "Control chars: \b\f",
						"arrayEmpty": [],
						"arrayOfPrimitives": [null, true, false, 42, "text", 3.14],
						"arrayNested": [
							[1, 2, 3],
							["a", "b", "c"],
							[{"key": "value"}]
						],
						"objectEmpty": {},
						"objectWithPrimitives": {
							"null": null,
							"bool": true,
							"number": 123,
							"string": "value"
						},
						"nestedObject": {
							"level1": {
								"level2": {
									"level3": {
										"deepValue": "I am nested"
									}
								}
							}
						},
						"objectWithArray": {
							"numbers": [1, 2, 3, 4, 5],
							"strings": ["apple", "banana", "cherry"],
							"mixed": [null, 42, "hello", true, {}]
						},
						"arrayOfObjects": [
							{"id": 1, "name": "Alice"},
							{"id": 2, "name": "Bob"},
							{"id": 3, "name": "Charlie", "active": true}
						],
						"mixedArray": [
							null,
							42,
							"string",
							true,
							false,
							[1, 2, 3],
							{"key": "value"},
							3.14159
						],
						"specialNumbers": {
							"positiveInfinity": null,
							"negativeInfinity": null,
							"NaN": null
						},
						"whitespaceTest": {
							"key with spaces": "value",
							"key_with_underscore": 123,
							"key-with-hyphen": true,
							"123numericKey": "allowed in values but not as pure keys in all parsers",
							"": "empty key is allowed in JSON"
						},
						"veryLongString": "This is a very long string to test how parsers handle large strings.",
						"unicodeSurrogatePairs": "😀 😂 ✝ ♰ 🇺🇸",
						"escapedUnicode": "\u2600 \u2601 \u2602",
						"commentsNote": "JSON does not support comments (this is just a string value)"
					}
					"""
				);
				break;
			}
			case "large":
			{
				using var rented = StringBuilderPool.Rent();
				using (var _ = ProfilerExtensions.Start(_textEditorRendererProfiler, "RandomGenerated"))
				{
					for (var i = 0; i < 25_000; i++)
					{
						RandomGenerator.LoremIpsum(rented.Value, maxSentences: 1);
						rented.Value.AppendLine();
					}
				}
				TextEditor.ViewModel.Load(rented.Value.ToString());
				break;
			}
			case "huge":
			{
				using var rented = StringBuilderPool.Rent();
				using (var _ = ProfilerExtensions.Start(_textEditorRendererProfiler, "RandomGenerated"))
				{
					for (var i = 0; i < 50_000; i++)
					{
						RandomGenerator.LoremIpsum(rented.Value, maxSentences: 1);
						rented.Value.AppendLine();
					}
				}
				TextEditor.ViewModel.Load(rented.Value.ToString());
				break;
			}
			case "text":
			{
				TextEditor.ViewModel.Load(
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
				TextEditor.ViewModel.ConfigureForFileType("xml");
				TextEditor.ViewModel.Load(
					"""
					<?xml version="1.0" encoding="UTF-8"?>
					<?xml-stylesheet type="text/xsl" href="style.xsl"?>
					<?php echo "This is a processing instruction with a non-standard target"; ?>
					<!DOCTYPE book [
					    <!ENTITY company "Doe Publishing">
					    <!ENTITY copy "©">
					    <!-- This is an internal DTD subset -->
					]>
					<!-- This is a top-level comment -->
					<book xmlns="http://example.com/ns" xmlns:dc="http://purl.org/dc/elements/1.1/" version="1.0">
					    <title>XML Tokenizer Test Document</title>
					    
					    <author>
					        <name>John &amp; Doe</name>
					        <email>john@example.com</email>
					    </author>

					    <chapter id="ch1" title='Chapter &quot;One&quot;' empty="">
					        <![CDATA[
					            This is CDATA content with <tags> & "quotes" that would normally break XML.
					        ]]>
					    </chapter>

					    <!-- Empty element test -->
					    <self-closing-tag attr1="value1" attr2='value2' attr3="with &amp; entity" flag="true" empty='' />

					    <mixed-content>
					        Text before &lt; &gt; &amp; &apos; &quot;
					        <inline-tag>Inline text with entity &copy;</inline-tag>
					        More text after.
					    </mixed-content>

					    <ns:qualified xmlns:ns="http://example.com/qualified">
					        This uses a namespace prefix.
					    </ns:qualified>

					    <!-- Testing whitespace variations -->
					    <whitespace-test    
					        attr-with-spaces   =   "value"   
					        single='value'
					    />

					</book>
					"""
				);
				break;
			}
		}

		TextEditor.Focus();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		DocumentChangedChart.ValueFormatter = HumanizeTimeSpan;
		LineManagerChart.ValueFormatter = HumanizeTimeSpan;
		TokenManagerChart.ValueFormatter = HumanizeTimeSpan;
		MeasureChart.ValueFormatter = HumanizeTimeSpan;
		RenderChart.ValueFormatter = HumanizeTimeSpan;
		RandomGeneratorChart.ValueFormatter = HumanizeTimeSpan;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			_timer.IsEnabled = true;
			_benchmark.DoWork += BenchmarkOnDoWork;
		}
		TextEditor.ViewModel.Caret.CaretMoved += CaretOnCaretMoved;
		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		TextEditor.ViewModel.Caret.CaretMoved -= CaretOnCaretMoved;
		_benchmark.DoWork -= BenchmarkOnDoWork;
		_timer.IsEnabled = false;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		TextEditor.Renderer.Profiler = _textEditorRendererProfiler;
		TextEditor.ViewModel.Profiler = _textEditorRendererProfiler;

		IsBenchmarking = false;
		_benchmark.CancelAsync();

		base.OnLoaded(e);
	}

	protected override void OnPropertyChanged(string propertyName)
	{
		if (propertyName == nameof(IsBenchmarking))
		{
			if (IsBenchmarking && !_benchmark.IsBusy)
			{
				_benchmark.RunWorkerAsync();
			}
			if (!IsBenchmarking && _benchmark.IsBusy)
			{
				_benchmark.CancelAsync();
			}
		}
		if (propertyName == nameof(SelectedToken))
		{
			var token = SelectedToken;
			if ((SelectedToken != null) && !_selectingDueToCaretMove)
			{
				TextEditor.ViewModel.Caret.Selection.Update(token.StartOffset, token.EndOffset);
			}
		}
		base.OnPropertyChanged(propertyName);
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		if ((Charts.GetValue(DockPanel.DockProperty) == Dock.Bottom)
			&& (Bounds.Width > 800))
		{
			Charts.SetValue(DockPanel.DockProperty, Dock.Right);
			Charts.ClearValue(MaxHeightProperty);
		}

		if ((Charts.GetValue(DockPanel.DockProperty) == Dock.Right)
			&& (Bounds.Width <= 800))
		{
			Charts.SetValue(DockPanel.DockProperty, Dock.Bottom);
			Charts.SetValue(MaxHeightProperty, 120);
		}

		base.OnSizeChanged(e);
	}

	private void BenchmarkOnDoWork(object sender, DoWorkEventArgs e)
	{
		// 78% of edits near last position
		const double localityChance = 0.78;

		var pasteBuffer = new StringGapBuffer(1024);
		const int targetDocumentLength = 100000;
		var lastOffset = 0;

		while (!_benchmark.CancellationPending)
		{
			if (!IsBenchmarking)
			{
				Thread.Sleep(10);
				continue;
			}

			Thread.Sleep(10);

			TextEditor.Dispatch(() =>
			{
				var length = TextEditor.ViewModel.DocumentLength;
				var offset = 0;

				// Prevents unbounded growth
				if (length > (targetDocumentLength * 1.4))
				{
					var trimAmount = (length - targetDocumentLength) + RandomGenerator.NextInteger(3000, 15000);
					offset = RandomGenerator.NextInteger(0, length - trimAmount);
					TextEditor.ViewModel.RemoveAt(offset, trimAmount);
					lastOffset = offset;
					return;
				}

				if (length == 0)
				{
					pasteBuffer.Clear();
					RandomGenerator.LoremIpsum(pasteBuffer, maxSentences: 1, numParagraphs: 1);
					TextEditor.ViewModel.Caret.Move(0);
					TextEditor.ViewModel.Insert(pasteBuffer);
					lastOffset = 0;
					return;
				}

				// Decide operation type (your original percentages unchanged)
				var roll = RandomGenerator.NextDouble();

				if (roll < 0.52)
				{
					// 52% – typing feel
					offset = GetBiasedOffset(lastOffset, length, localityChance);
					var c = RandomGenerator.GetItem(RandomGenerator.AlphabetNumbersAndSymbols);
					TextEditor.ViewModel.Caret.Move(offset);
					TextEditor.ViewModel.Insert(c);
				}
				else if (roll < 0.75)
				{
					// 23% – backspace / small delete
					offset = GetBiasedOffset(lastOffset, length, localityChance);
					var delLen = Math.Min(RandomGenerator.NextInteger(1, 6), length - offset);
					TextEditor.ViewModel.RemoveAt(offset, delLen);
				}
				else if (roll < 0.88)
				{
					// 13% – medium paste (now adaptive)
					offset = GetBiasedOffset(lastOffset, length, localityChance);
					pasteBuffer.Clear();

					// smaller pastes when already big
					var paragraphs = length > 60000 ? 1 : 3;
					RandomGenerator.LoremIpsum(pasteBuffer, maxSentences: 2, numParagraphs: paragraphs);
					TextEditor.ViewModel.Caret.Move(offset);
					TextEditor.ViewModel.Insert(pasteBuffer);
				}
				else if (roll < 0.96)
				{
					// 8% – big delete range
					offset = RandomGenerator.NextInteger(0, length);
					var delLen = Math.Min(RandomGenerator.NextInteger(20, 200), length - offset);
					TextEditor.ViewModel.RemoveAt(offset, delLen);
				}
				else
				{
					// 4% – replace
					offset = GetBiasedOffset(lastOffset, length, localityChance);
					var repLen = Math.Min(RandomGenerator.NextInteger(5, 30), length - offset);
					var replacement = RandomGenerator.NextString(RandomGenerator.NextInteger(8, 25));
					TextEditor.ViewModel.RemoveAt(offset, repLen);
					TextEditor.ViewModel.Insert(replacement);
				}

				lastOffset = offset;
			});
		}
	}

	private void CaretOnCaretMoved(object sender, EventArgs e)
	{
		var vm = TextEditor.ViewModel;
		var token = vm.TokenManager.FirstOrDefault(x => x.Contains(vm.Caret.Offset));
		if (token == null)
		{
			return;
		}
		_selectingDueToCaretMove = true;
		TokenItems.SelectedItem = token;
		_selectingDueToCaretMove = false;
	}

	private int GetBiasedOffset(int lastOffset, int length, double localityChance)
	{
		if ((RandomGenerator.NextDouble() < localityChance) && (length > 50))
		{
			var delta = RandomGenerator.NextInteger(-150, 151);
			return Math.Clamp(lastOffset + delta, 0, length);
		}

		// full random teleport (stresses gap buffer)
		return RandomGenerator.NextInteger(0, length + 1);
	}

	private static string HumanizeTimeSpan(double value)
	{
		return TimeSpan.FromTicks((long) value).Humanize();
	}

	private void ProviderUpdate(object sender, EventArgs e)
	{
		_textEditorRendererProfiler.Refresh();
	}

	#endregion
}