#region References

using System.Linq;
using Avalonia.Input;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.Avalonia.TextEditor.CodeCompletion;

public class CompletionProvider : Bindable, ICompletionProvider
{
	#region Constructors

	public CompletionProvider(TextEditorDocument document, IDispatcher dispatcher, params CompletionTrigger[] keys) : base(dispatcher)
	{
		Document = document;
		Keys = keys;
		Data = new SpeedyList<ICompletionData>(dispatcher,
			new OrderBy<ICompletionData>(x => x.Priority),
			new OrderBy<ICompletionData>(x => x.DisplayText)
		)
		{
			FilterCheck = x => x.Priority < 100
		};
	}

	#endregion

	#region Properties

	public SpeedyList<ICompletionData> Data { get; }

	public TextEditorDocument Document { get; }

	public CompletionTrigger[] Keys { get; }

	public IRange TextToReplace { get; protected set; }

	#endregion

	#region Methods

	public virtual string GetTextToReplace()
	{
		return string.Empty;
	}

	public virtual IRange GetTextToReplaceWithCompletionResults(string input, int offset, string completionText)
	{
		return new SimpleRange();
	}

	public virtual bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
	{
		var found = Keys
			.FirstOrDefault(x =>
				(x.Key == key) &&
				(x.Modifiers == modifiers)
			);

		silent = found.Silent;
		return found.Key != Key.None;
	}

	public virtual bool TryGetAutoComplete(IRange inputRange, int caretOffset)
	{
		return false;
	}

	#endregion
}

public interface ICompletionProvider
{
	#region Properties

	SpeedyList<ICompletionData> Data { get; }

	IRange TextToReplace { get; }

	#endregion

	#region Methods

	string GetTextToReplace();

	bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent);

	bool TryGetAutoComplete(IRange inputRange, int caretOffset);

	#endregion
}