#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
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

	#region Constructors

	public TabSecurityKeys()
	{
		DataContext = this;
		SmartCardReader = GetInstance<SmartCardReader>();

		InitializeComponent();
	}

	#endregion

	#region Properties

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

	private void ClearOnClick(object sender, RoutedEventArgs e)
	{
		Output.Clear();
	}

	private void RefreshOnClick(object sender, RoutedEventArgs e)
	{
		SmartCardReader.Card?.Refresh();
	}

	private void SmartCardReaderOnCardInserted(object sender, SecurityCard e)
	{
		this.Dispatch(() =>
		{
			var data = SmartCardReader.Card?.Data;
			if (data != null)
			{
				for (var i = 0; i < data.Length; i += 16)
				{
					var remaining = Math.Min(16, data.Length - i);
					var block = new byte[remaining];
					Array.Copy(data, i, block, 0, remaining);
					Output.AppendText(block.ToHexString());
					Output.AppendText(Environment.NewLine);
				}
			}

			Output.AppendText("+++ ");
			Output.AppendText(e.UniqueId);
			Output.AppendText(Environment.NewLine);
		});
	}

	private void SmartCardReaderOnCardRemoved(object sender, SecurityCard e)
	{
		this.Dispatch(() =>
		{
			Output.AppendText("--- ");
			Output.AppendText(e.UniqueId);
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

	#endregion
}