#region References

using System;
using Cornerstone.Data;
using Cornerstone.Parsers;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

[SourceReflection]
public class IndentionManager : Notifiable
{
	#region Fields

	private IndentionService _indentionService;
	private readonly TextEditorViewModel _viewModel;

	#endregion

	#region Constructors

	public IndentionManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
	}

	#endregion

	#region Properties

	public bool HasIndentionService => _indentionService != null;

	public string IndentString => _indentionService?.Indent ?? "\t";

	#endregion

	#region Methods

	public ReadOnlySpan<char> GetIndentLevel(int offset)
	{
		return TryGetIndention(offset, out var indention) ? indention : Span<char>.Empty;
	}

	public void Initialize(string extension)
	{
		Initialize(IndentionService.GetByExtension(extension, _viewModel.Buffer));
	}

	public void Initialize(IndentionService completionService)
	{
		_indentionService = completionService;
		OnPropertyChanged(nameof(HasIndentionService));
	}

	public bool TryGetIndention(int offset, out ReadOnlySpan<char> indention)
	{
		indention = default;
		return _indentionService is not null
			&& _viewModel.Lines.TryGetLineForOffset(offset, out var currentLine)
			&& _viewModel.Lines.TryGetLine(currentLine.LineNumber - 1, out var previousLine)
			&& _indentionService.TryGetIndent(previousLine, currentLine, out indention);
	}

	#endregion
}