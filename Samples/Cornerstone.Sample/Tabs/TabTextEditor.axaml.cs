#region References

using Avalonia.Interactivity;
using Cornerstone.Avalonia;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabTextEditor : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "TextEditor";

	#endregion

	#region Constructors

	public TabTextEditor()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		CSharp.SetSyntaxHighlighterByExtension(".cs");
		CSharp.Text = """
					public class Test
					{
						public Test()
						{
						}
						
						public bool Enabled { get; set; }
						
						public void Start()
						{
						}
					}
					""";

		Html.SetSyntaxHighlighterByExtension(".json");
		Html.Text = """
					<!DOCTYPE html>
					<html lang="en">
						<head>
							<meta charset="utf-8" />
							<title>Website</title>
						</style>
						</head>
						<body>
							<div>
								The quick brown fox...
							</div>
						</body>
					</html>
					""";

		Json.SetSyntaxHighlighterByExtension(".json");
		Json.Text = """
					{
						"Name": "John Doe",
						"Age": 21
					}
					""";

		PowerShell.SetSyntaxHighlighterByExtension(".ps1");
		PowerShell.Text = """
						$file = Get-Content -Path "test.sql" -Raw
						Invoke-SqlNonQuery -Database "master" -Query $file -QueryTimeout 60
						""";

		Xml.SetSyntaxHighlighterByExtension(".xml");
		Xml.Text = """
					<Account Age="21" Name="John Doe">
						<Address>123 Main Street</Address>
					</Account>
					""";

		base.OnLoaded(e);
	}

	#endregion
}