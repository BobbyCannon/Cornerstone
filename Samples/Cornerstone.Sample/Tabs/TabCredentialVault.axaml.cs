#region References

using System;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Security;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabCredentialVault : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "CredentialVault";

	#endregion

	#region Constructors

	public TabCredentialVault()
	{
		CredentialVault = GetInstance<CredentialVault>();
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public CredentialVault CredentialVault { get; }

	#endregion

	#region Methods

	private void ClearCredential(object sender, RoutedEventArgs e)
	{
		var result = CredentialVault.RemoveCredential();
		History.AppendText(result
			? "Cleared the credential from the vault."
			: "Failed to clear the credential."
		);
		History.AppendText(Environment.NewLine);
	}

	private void ClearHistory(object sender, RoutedEventArgs e)
	{
		History.Clear();
	}

	private void ReadCredential(object sender, RoutedEventArgs e)
	{
		var result = CredentialVault.LoadCredential();
		History.AppendText(result
			? "Read the credential from the vault."
			: "Failed to read the credential."
		);
		History.AppendText(Environment.NewLine);
	}

	private void WriteCredential(object sender, RoutedEventArgs e)
	{
		var result = CredentialVault.SaveCredential();
		History.AppendText(result
			? "Wrote the credential to the vault."
			: "Failed to write the credential."
		);
		History.AppendText(Environment.NewLine);
	}

	#endregion
}