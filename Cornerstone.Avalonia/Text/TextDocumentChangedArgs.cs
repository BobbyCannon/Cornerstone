#region References

using System;

#endregion

namespace Cornerstone.Avalonia.Text;

public class TextDocumentChangedArgs : EventArgs
{
	#region Properties

	public int Length { get; set; }

	public int Offset { get; set; }

	#endregion
}