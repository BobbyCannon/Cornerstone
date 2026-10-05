#region References

using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Models;

/// <summary>
/// Diagnostic spans for squiggle paint. Replaced as a snapshot; not a live tokenizer.
/// </summary>
[SourceReflection]
public partial class DiagnosticManager : CornerstoneObject
{
	#region Fields

	private readonly TextEditorViewModel _viewModel;
	private TextDiagnostic[] _items;

	#endregion

	#region Constructors

	public DiagnosticManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
		_items = [];
	}

	#endregion

	#region Properties

	public IReadOnlyList<TextDiagnostic> Items => _items;

	[Notify]
	public partial int ErrorCount { get; private set; }

	[Notify]
	public partial int Version { get; private set; }

	[Notify]
	public partial int WarningCount { get; private set; }

	#endregion

	#region Methods

	public void Clear()
	{
		Replace([]);
	}

	public void Replace(IReadOnlyList<TextDiagnostic> items)
	{
		_items = items == null || items.Count == 0 ? [] : [..items];
		var errors = 0;
		var warnings = 0;
		for (var i = 0; i < _items.Length; i++)
		{
			if (_items[i].IsError)
			{
				errors++;
			}
			else
			{
				warnings++;
			}
		}

		ErrorCount = errors;
		WarningCount = warnings;
		Version++;
		_viewModel?.NotifyDiagnosticVersion(Version);
	}

	#endregion
}
