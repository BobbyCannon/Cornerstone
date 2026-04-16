#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Text;
using Cornerstone.Data;
using Cornerstone.Generators;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;

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

		var view = new MarkdownView
		{
			Markdown = "Streaming...";
		};
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

	private readonly BackgroundWorker _benchmark;
	private readonly IStringBuffer _markdownBuffer;
	private readonly MarkdownGenerator _markdownGenerator;
	private CancellationTokenSource _sampleLoopToken;

	#endregion

	#region Constructors

	public TabMarkdownView() : this(CornerstoneApplication.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabMarkdownView(IRuntimeInformation runtimeInformation)
	{
		_benchmark = new BackgroundWorker();
		_benchmark.WorkerReportsProgress = true;
		_benchmark.WorkerSupportsCancellation = true;
		_markdownBuffer = new StringBuffer();
		_markdownGenerator = new MarkdownGenerator();

		RuntimeInformation = runtimeInformation;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool IsBenchmarking { get; set; }

	[Notify]
	public partial bool LoopSample { get; set; }

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			_benchmark.DoWork += BenchmarkOnDoWork;
			_benchmark.ProgressChanged += BenchmarkOnProgressChanged;
		}
		base.OnAttachedToVisualTree(e);
		TextEditor.ViewModel.DocumentChanged += ViewModelOnDocumentChanged;
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			_benchmark.DoWork -= BenchmarkOnDoWork;
			_benchmark.ProgressChanged -= BenchmarkOnProgressChanged;
		}
		TextEditor.ViewModel.DocumentChanged -= ViewModelOnDocumentChanged;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		TextEditor.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
		TextEditor.Text = SampleMarkdown;
		base.OnLoaded(e);
	}

	protected override void OnPropertyChanged(string propertyName)
	{
		if (propertyName == nameof(IsBenchmarking))
		{
			if (IsBenchmarking && !_benchmark.IsBusy)
			{
				TextEditor.ViewModel.Clear();
				_markdownGenerator.Reset();
				_benchmark.RunWorkerAsync();
			}
			if (!IsBenchmarking && _benchmark.IsBusy)
			{
				_benchmark.CancelAsync();

				// Force garbage collection (for testing / benchmarking only!)
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
				GC.WaitForPendingFinalizers();
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
			}
		}
		base.OnPropertyChanged(propertyName);
	}

	private void BenchmarkOnDoWork(object sender, DoWorkEventArgs e)
	{
		while (!_benchmark.CancellationPending)
		{
			if (!IsBenchmarking)
			{
				Thread.Sleep(10);
				continue;
			}

			lock (_markdownBuffer)
			{
				_markdownGenerator.GetNext(_markdownBuffer);
				_benchmark.ReportProgress(0);
			}
		}
	}

	private void BenchmarkOnProgressChanged(object sender, ProgressChangedEventArgs e)
	{
		lock (_markdownBuffer)
		{
			TextEditor.ViewModel.Append(_markdownBuffer.ToString());
			TextEditor.ScrollToEnd();
			MarkdownView.ScrollToEnd();
		}
	}

	private async void RunSampleStream(object sender, RoutedEventArgs e)
	{
		// Cancel any previous run first
		_sampleLoopToken?.Cancel();
		_sampleLoopToken?.Dispose();
		_sampleLoopToken = new CancellationTokenSource();

		var token = _sampleLoopToken.Token;

		try
		{
			do
			{
				await RunSingleStreamPass(token);

				// Only delay between full loops if we're looping
				if (LoopSample && !token.IsCancellationRequested)
				{
					await Task.Delay(500, token);
				}
			} while (LoopSample && !token.IsCancellationRequested);
		}
		catch (OperationCanceledException)
		{
			// Expected when stopping
		}
		catch (Exception ex)
		{
			// Log unexpected error
			Debug.WriteLine($"Streaming error: {ex}");
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

		var chunks = SplitIntoStreamingChunks(SampleMarkdown, 60, 15);

		foreach (var chunk in chunks)
		{
			token.ThrowIfCancellationRequested();

			TextEditor.ViewModel.Append(chunk);

			await Task.Delay(RandomGenerator.NextInteger(10, 50), token);

			TextEditor.ScrollToEnd();
			MarkdownView.ScrollToEnd();
		}
	}

	private static List<string> SplitIntoStreamingChunks(string text, int chunkSize = 60, int minChunk = 15)
	{
		var chunks = new List<string>();
		var position = 0;

		while (position < text.Length)
		{
			var remaining = text.Length - position;
			var currentChunkSize = Math.Min(chunkSize, remaining);

			// Occasionally make smaller or larger chunks (more natural)
			if (Random.Shared.Next(0, 10) < 3)
			{
				currentChunkSize = Math.Max(minChunk, currentChunkSize / 2);
			}

			var length = Math.Min(remaining, currentChunkSize);
			var chunk = text.Substring(position, length);

			// Try to avoid cutting words in half when possible (better visual flow)
			if (((position + length) < text.Length) && !char.IsWhiteSpace(chunk[^1]))
			{
				var lastSpace = chunk.LastIndexOf(' ');
				if (lastSpace > (length / 2))
				{
					length = lastSpace + 1;
				}
			}

			chunks.Add(text.Substring(position, length));
			position += length;
		}

		return chunks;
	}

	private void ViewModelOnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		MarkdownView.Markdown = TextEditor.Text;
	}

	#endregion
}