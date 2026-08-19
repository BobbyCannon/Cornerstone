#region References

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Text;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabMarkdownView : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Markdown View";

	public const string SampleMarkdown =
		"""
		# 🚀 Ultimate Markdown Stress Test: Live AI Streaming Demo

		**Hello from your AI assistant!** *This entire response is streaming in real-time* — watch your Avalonia `MarkdownView` handle progressive updates, reflows, and rich formatting without breaking a sweat.

		## Typography & Inline Magic
		**Bold text** · *Italic text* · ***Bold + italic*** · ~~Strikethrough~~ · `inline code` · [Hyperlink to Avalonia](https://avaloniaui.net)

		Normal paragraph with mixed styles. This long one tests word-wrapping and live reflow: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.

		> # Block Quotes
		> Should be able to go cross 
		> many lines for the quote.

		## Lists (Nested + Task Lists)
		### Unordered
		- Item 1 with **bold**
		  - Nested unordered item
		    - Even deeper
		- Item 2 with `code` and ~~strikethrough~~

		### Task List
		- [x] Completed feature: headings
		- [ ] Pending: full table rendering
		- [x] Done: live streaming support

		## Code Blocks
		```csharp

		var view = new MarkdownView();
		view.Document.Load("Streaming...");
		```

		```json
		{
			"Name": "John",
			"Age": 21
		}
		```

		```markdown
		# Header
		---
		1. Options One
		1. Options Two
		```

		```xml
		<Account "Name"="John" "Age"="21">
			<Address "Number"="123" "Street"="Main" />
		</Account>
		```

		## Tables (Stress Test)
		| Feature | Description | Status | Version | Performance |
		|-|-|-|:-:|-:|
		| Zero-Allocation Parsing | Computes widths without storing strings | Stable | 1.2.3 | Excellent |
		| Alignment Support | Left, center, and right alignment | Complete | 2.0 | Very Fast |
		| Large Table Handling | Handles thousands of rows efficiently| Beta | 1.5 | Outstanding |
		| StringBuilder Optimization | Minimal resizes and fast appends | Stable | 1.0 | High |
		| .NET 10 Span Usage | Modern low-allocation APIs | Released | 3.1 | Best |

		## Math
		Inline: $E = mc^2$

		Display:
		$$
		\sum_{i=1}^{n} i = \frac{n(n+1)}{2}
		$$

		*Streaming complete! This tests incremental parsing, reflow, tables, math, and more.*
		""";

	#endregion

	#region Fields

	/// <summary>
	/// Combo options for stream speed (Slow → Extreme).
	/// </summary>
	public static readonly MarkdownStreamSpeed[] StreamSpeeds =
	[
		MarkdownStreamSpeed.Slow,
		MarkdownStreamSpeed.Normal,
		MarkdownStreamSpeed.Fast,
		MarkdownStreamSpeed.Extreme
	];

	private CancellationTokenSource _sampleLoopToken;

	#endregion

	#region Constructors

	public TabMarkdownView() : this(AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabMarkdownView(IRuntimeInformation runtimeInformation)
	{
		RuntimeInformation = runtimeInformation;
		StreamSpeed = MarkdownStreamSpeed.Normal;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool LoopSample { get; set; }

	public IRuntimeInformation RuntimeInformation { get; }

	/// <summary>
	/// How quickly tokens are appended during Sample Stream.
	/// </summary>
	[Notify]
	public partial MarkdownStreamSpeed StreamSpeed { get; set; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		TextEditor.ViewModel.DocumentChanged += ViewModelOnDocumentChanged;
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		TextEditor.ViewModel.DocumentChanged -= ViewModelOnDocumentChanged;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		TextEditor.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

		// Same normalization as stream so line count matches a finished Sample Stream.
		TextEditor.Text = NormalizeNewlines(SampleMarkdown);
		base.OnLoaded(e);
	}

	/// <summary>
	/// Choose a stable end offset within [minChunk, chunkSize], preferring \n then space.
	/// </summary>
	private static int FindChunkLength(string text, int position, int chunkSize, int minChunk)
	{
		var end = position + chunkSize;

		// Prefer break after the last newline in the window (include the newline).
		for (var i = end - 1; i >= (position + minChunk); i--)
		{
			if (text[i] == '\n')
			{
				return (i + 1) - position;
			}
		}

		// Else last space / tab in the window.
		for (var i = end - 1; i >= (position + minChunk); i--)
		{
			var c = text[i];
			if ((c == ' ') || (c == '\t'))
			{
				return (i + 1) - position;
			}
		}

		var length = chunkSize;

		// Never leave a lone \r if the next char is \n (would invent an extra line).
		if (((position + length) < text.Length)
			&& (text[(position + length) - 1] == '\r')
			&& (text[position + length] == '\n'))
		{
			length++;
		}

		return length;
	}

	/// <summary>
	/// Fixed delay + chunk size per speed. Extreme has no delay and large chunks.
	/// </summary>
	private static (int DelayMilliseconds, int ChunkSize, int MinChunk, int LoopPause) GetStreamTiming(
		MarkdownStreamSpeed speed)
	{
		return speed switch
		{
			MarkdownStreamSpeed.Slow => (120, 28, 10, 800),
			MarkdownStreamSpeed.Fast => (8, 100, 40, 200),
			MarkdownStreamSpeed.Extreme => (0, 280, 120, 50),
			_ => (30, 60, 15, 500)
		};
	}

	private static string NormalizeNewlines(string text)
	{
		return string.IsNullOrEmpty(text)
			? string.Empty
			: text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
	}

	private async void RunSampleStream(object sender, RoutedEventArgs e)
	{
		try
		{
			// Cancel any previous run first
			_sampleLoopToken?.Cancel();
			_sampleLoopToken?.Dispose();
			_sampleLoopToken = new CancellationTokenSource();

			var token = _sampleLoopToken.Token;
			do
			{
				await RunSingleStreamPass(token);

				// Pause between full loops when looping (scaled by speed).
				if (LoopSample && !token.IsCancellationRequested)
				{
					var loopPause = GetStreamTiming(StreamSpeed).LoopPause;
					if (loopPause > 0)
					{
						await Task.Delay(loopPause, token);
					}
				}
			} while (LoopSample && !token.IsCancellationRequested);
		}
		catch (OperationCanceledException)
		{
			// Expected when stopping
		}
		catch (Exception)
		{
			// Ignore unexpected stream errors in the demo tab
		}
		finally
		{
			_sampleLoopToken?.Dispose();
			_sampleLoopToken = null;
		}
	}

	private async Task RunSingleStreamPass(CancellationToken token)
	{
		TextEditor.Text = string.Empty;

		// Same text every run (LF-normalized) so line counts never drift.
		var source = NormalizeNewlines(SampleMarkdown);
		var timing = GetStreamTiming(StreamSpeed);
		var chunks = SplitIntoStreamingChunks(source, timing.ChunkSize, timing.MinChunk);

		foreach (var chunk in chunks)
		{
			token.ThrowIfCancellationRequested();

			TextEditor.ViewModel.Append(chunk);

			// Fixed delay per speed (not random) so pacing is repeatable too.
			if (timing.DelayMilliseconds > 0)
			{
				await Task.Delay(timing.DelayMilliseconds, token);
			}

			TextEditor.ScrollToEnd();
			MarkdownView.ScrollToEnd();
		}
	}

	/// <summary>
	/// Deterministic chunks: same input + size always yields the same split.
	/// Prefers breaks after newlines, then spaces; never splits a CRLF pair.
	/// </summary>
	private static List<string> SplitIntoStreamingChunks(string text, int chunkSize, int minChunk)
	{
		var chunks = new List<string>();
		if (string.IsNullOrEmpty(text))
		{
			return chunks;
		}

		chunkSize = Math.Max(1, chunkSize);
		minChunk = Math.Clamp(minChunk, 1, chunkSize);
		var position = 0;

		while (position < text.Length)
		{
			var remaining = text.Length - position;
			if (remaining <= chunkSize)
			{
				chunks.Add(text[position..]);
				break;
			}

			var length = FindChunkLength(text, position, chunkSize, minChunk);
			chunks.Add(text.Substring(position, length));
			position += length;
		}

		return chunks;
	}

	private void ViewModelOnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		// Mirror the editor document into the markdown view document (owned separately).
		// Streaming demos use Append; full resets use Load.
		if (e.Type == TextDocumentChangeType.Reset)
		{
			MarkdownView.Document.Load(TextEditor.ViewModel.ToString());
		}
		else if (e.Type == TextDocumentChangeType.Add)
		{
			if (e.Offset >= MarkdownView.Document.DocumentLength)
			{
				MarkdownView.Document.Append(e.Text ?? string.Empty);
			}
			else
			{
				MarkdownView.Document.Load(TextEditor.ViewModel.ToString());
			}
		}
		else
		{
			MarkdownView.Document.Load(TextEditor.ViewModel.ToString());
		}
	}

	#endregion
}

/// <summary>
/// How quickly the Sample Stream demo feeds tokens into the editor / MarkdownView.
/// </summary>
public enum MarkdownStreamSpeed
{
	Slow = 0,
	Normal = 1,
	Fast = 2,
	Extreme = 3
}