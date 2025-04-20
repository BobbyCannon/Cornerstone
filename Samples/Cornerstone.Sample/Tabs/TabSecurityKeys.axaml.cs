#region References

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.HexEditor.Document;
using Cornerstone.Avalonia.HexEditor.Rendering;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Security.SecurityKeys;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabSecurityKeys : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Security Keys";

	#endregion

	#region Fields

	private readonly RangesHighlighter _changesHighlighter;
	private readonly InvalidRangesHighlighter _invalidRangesHighlighter;

	#endregion

	#region Constructors

	public TabSecurityKeys()
	{
		DataContext = this;
		Block = 0;
		MaximumDataToRead = 64;
		SmartCardReader = GetInstance<SmartCardReader>();

		InitializeComponent();

		_changesHighlighter = new RangesHighlighter { Foreground = Brushes.Red };
		_invalidRangesHighlighter = new InvalidRangesHighlighter
		{
			Foreground = new SolidColorBrush(Colors.Gray, 0.5)
		};

		// Enable the changes highlighter.
		HexEditor.HexView.LineTransformers.Add(_changesHighlighter);
		HexEditor.HexView.LineTransformers.Add(_invalidRangesHighlighter);
		HexEditor.HexView.BytesPerLine = 16;

		// Divide each 8 bytes with a dashed line and separate colors.
		var layer = HexEditor.HexView.Layers.Get<CellGroupsLayer>();
		layer.Backgrounds.Add(new SolidColorBrush(Colors.Gray, 0.1D));
		layer.Backgrounds.Add(null);
		layer.Border = new Pen(Brushes.Gray, dashStyle: DashStyle.Dash);
		layer.BytesPerGroup = 8;

		HexEditor.DocumentChanged += HexEditorOnDocumentChanged;
	}

	#endregion

	#region Properties

	public int Block { get; set; }

	public string BlockData { get; set; }

	public int MaximumDataToRead { get; set; }

	public SmartCardReader SmartCardReader { get; }

	public bool VerboseLogging { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			SmartCardReader.Initialize();
			SmartCardReader.CardInserted += SmartCardReaderOnCardInserted;
			SmartCardReader.CardRemoved += SmartCardReaderOnCardRemoved;
			SmartCardReader.WriteLine += SmartCardReaderOnWriteLine;
		}
		base.OnAttachedToVisualTree(e);
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			SmartCardReader.Uninitialize();
			SmartCardReader.CardInserted -= SmartCardReaderOnCardInserted;
			SmartCardReader.CardRemoved -= SmartCardReaderOnCardRemoved;
			SmartCardReader.WriteLine -= SmartCardReaderOnWriteLine;
		}
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		var buffer = new byte[1024];
		var document = new DynamicBinaryDocument(buffer);
		HexEditor.Document = document;

		if (SmartCardReader.Card != null)
		{
			document.Write(0, SmartCardReader.Card.Data);
			_changesHighlighter.Ranges.Clear();
		}

		base.OnLoaded(e);
	}

	private void ClearOnClick(object sender, RoutedEventArgs e)
	{
		Output.Clear();
	}

	private void DocumentOnChanged(object sender, BinaryDocumentChange change)
	{
		switch (change.Type)
		{
			case BinaryDocumentChangeType.Modify:
			{
				_changesHighlighter.Ranges.Add(change.AffectedRange);
				break;
			}
			case BinaryDocumentChangeType.Insert:
			case BinaryDocumentChangeType.Remove:
			{
				_changesHighlighter.Ranges.Add(change.AffectedRange.ExtendTo(HexEditor.Document!.ValidRanges.EnclosingRange.End));
				break;
			}
			default:
			{
				Debugger.Break();
				break;
			}
		}
	}

	private void HexEditorOnDocumentChanged(object sender, DocumentChangedEventArgs e)
	{
		_changesHighlighter.Ranges.Clear();

		if (e.Old is not null)
		{
			e.Old.Changed -= DocumentOnChanged;
		}
		if (e.New is not null)
		{
			e.New.Changed += DocumentOnChanged;
		}
	}

	private void ReadBlock(object sender, RoutedEventArgs e)
	{
		var data = SmartCardReader.Card?.ReadBlock((ushort) Block);
		if (data != null)
		{
			BlockData = data.ToHexString();
		}
		else
		{
			Output.AppendText($"Error reading from block {Block}.");
			Output.AppendText(Environment.NewLine);
		}
	}

	private void RefreshCardOnClick(object sender, RoutedEventArgs e)
	{
		SmartCardReader.Card?.Refresh();
	}

	private void RefreshReadersOnClick(object sender, RoutedEventArgs e)
	{
		SmartCardReader.RefreshReadersAsync();
	}

	private void SmartCardReaderOnCardInserted(object sender, SecurityCard e)
	{
		this.Dispatch(() =>
		{
			var data = SmartCardReader.Card?.Data;
			if (data != null)
			{
				var maxLength = Math.Min(MaximumDataToRead, data.Length);

				for (var i = 0; i < maxLength; i += 16)
				{
					var remaining = Math.Min(16, data.Length - i);
					var block = new byte[remaining];
					Array.Copy(data, i, block, 0, remaining);
				}
			}

			var document = HexEditor.Document;
			document.Write(0, data);

			_changesHighlighter.Ranges.Clear();

			Output.AppendText("+++ ");
			Output.AppendText(e.UniqueId);
			Output.AppendText(Environment.NewLine);
		});
	}

	private void SmartCardReaderOnCardRemoved(object sender, SecurityCard e)
	{
		this.Dispatch(() =>
		{
			if (e?.Data != null)
			{
				var document = HexEditor.Document;
				for (var i = 0; i < e.Data.Length; i++)
				{
					e.Data[i] = 0;
				}
				document.Write(0, e.Data);
			}

			BlockData = string.Empty;

			_changesHighlighter.Ranges.Clear();
			
			Output.AppendText("--- ");
			Output.AppendText(e?.UniqueId);
			Output.AppendText(Environment.NewLine);
		});
	}

	private void SmartCardReaderOnWriteLine(object sender, string message)
	{
		if (!VerboseLogging)
		{
			return;
		}

		this.Dispatch(() =>
		{
			Output.AppendText(message);
			Output.AppendText(Environment.NewLine);
		});
	}

	private void WriteBlock(object sender, RoutedEventArgs e)
	{
		var data = BlockData.FromHexStringToByteArray();
		data = SmartCardReader.Card.WriteBlock((ushort) Block, data);
	}

	#endregion
}