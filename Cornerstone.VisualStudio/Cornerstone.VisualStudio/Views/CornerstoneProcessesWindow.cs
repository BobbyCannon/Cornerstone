#region References

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

#endregion

namespace Cornerstone.VisualStudio.Views;

[Guid("c4a91e27-6f38-4b0d-9e55-81d2a7c04f16")]
internal sealed class CornerstoneProcessesWindow : ToolWindowPane
{
	public CornerstoneProcessesWindow()
		: base(null)
	{
		Caption = "Cornerstone";
		Content = new CornerstoneProcessesView();
	}
}
