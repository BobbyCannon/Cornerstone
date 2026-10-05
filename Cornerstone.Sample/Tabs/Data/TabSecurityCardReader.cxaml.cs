#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Security.SecurityKeys;

#endregion

namespace Cornerstone.Sample.Tabs.Data;

[SourceReflection]
public partial class TabSecurityCardReader : SampleUserControl
{
	#region Constants

	public const string HeaderName = "Security Card Reader";

	#endregion

	#region Constructors

	public TabSecurityCardReader()
		: this(AppBootstrap.GetInstance<SecurityCardReader>(), AppBootstrap.GetInstance<IPermissions>())
	{
	}

	[DependencyInjectionConstructor]
	public TabSecurityCardReader(SecurityCardReader securityCardReader, IPermissions permissions)
	{
		SecurityCardReader = securityCardReader;
		Permissions = permissions;
		DataContext = this;
		InitializeComponent();
		RefreshCardDisplay();
	}

	#endregion

	#region Properties

	[Notify]
	public partial string CardDataHex { get; private set; }

	[Notify]
	public partial string CardTypeName { get; private set; }

	[Notify]
	public partial bool HasCard { get; private set; }

	public IPermissions Permissions { get; }

	public SecurityCardReader SecurityCardReader { get; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			SecurityCardReader.CardInserted += SecurityCardReaderOnCardChanged;
			SecurityCardReader.CardRemoved += SecurityCardReaderOnCardChanged;
			_ = Permissions.RequestPermissionAsync(PermissionType.NearFieldCommunications);
			SecurityCardReader.InitializeLifecycle();

			if (!SecurityCardReader.IsListening)
			{
				SecurityCardReader.StartListening();
			}

			RefreshCardDisplay();
		}

		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			if (SecurityCardReader.IsListening)
			{
				SecurityCardReader.StopListening();
			}

			SecurityCardReader.UninitializeLifecycle();
			SecurityCardReader.CardInserted -= SecurityCardReaderOnCardChanged;
			SecurityCardReader.CardRemoved -= SecurityCardReaderOnCardChanged;
		}

		base.OnDetachedFromVisualTree(e);
	}

	private void ClearCardOnClick(object sender, RoutedEventArgs e)
	{
		SecurityCardReader.ClearCard();
		RefreshCardDisplay();
	}

	private void ReadCardOnClick(object sender, RoutedEventArgs e)
	{
		SecurityCardReader.ReadCard();
		RefreshCardDisplay();
	}

	private void RefreshCardDisplay()
	{
		var card = SecurityCardReader.Card;
		HasCard = card != null;
		CardTypeName = card == null ? "None" : card.ToString();
		CardDataHex = (card?.Data == null) || (card.Data.Length == 0)
			? string.Empty
			: BitConverter.ToString(card.Data);
	}

	private void RefreshOnClick(object sender, RoutedEventArgs e)
	{
		_ = SecurityCardReader.RefreshAsync();
		RefreshCardDisplay();
	}

	private void SecurityCardReaderOnCardChanged(object sender, SecurityCard e)
	{
		this.Dispatch(RefreshCardDisplay);
	}

	private void WriteCardOnClick(object sender, RoutedEventArgs e)
	{
		SecurityCardReader.WriteCard();
		RefreshCardDisplay();
	}

	#endregion
}
