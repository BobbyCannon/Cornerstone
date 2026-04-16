#region References

using System;
using Cornerstone.Data;
using Cornerstone.Parsers;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

[SourceReflection]
public partial class CompletionManager : Notifiable
{
	#region Fields

	private CompletionService _completionService;
	private readonly TextEditorViewModel _viewModel;

	#endregion

	#region Constructors

	public CompletionManager(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
	}

	#endregion

	#region Properties

	public bool HasCompletionService => _completionService != null;

	#endregion

	#region Methods

	public void Initialize(string extension)
	{
		Initialize(CompletionService.GetByExtension(extension));
	}

	public void Initialize(CompletionService completionService)
	{
		_completionService = completionService;
		OnPropertyChanged(nameof(HasCompletionService));
	}

	public bool TryGetCompletion(ReadOnlySpan<char> input, out ReadOnlySpan<char> completion)
	{
		if (_completionService is null)
		{
			completion = default;
			return false;
		}

		return _completionService.TryGetCompletion(input, out completion);
	}

	#endregion
}