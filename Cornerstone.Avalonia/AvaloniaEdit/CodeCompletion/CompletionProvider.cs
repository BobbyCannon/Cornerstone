#region References

using System.Linq;
using Avalonia.Input;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;

public abstract class CompletionProvider : Bindable, ICompletionProvider
{
	#region Constructors

	protected CompletionProvider(IDispatcher dispatcher, params CompletionTrigger[] keys) : base(dispatcher)
	{
		Keys = keys;
		Data = CreateList(dispatcher);
	}

	#endregion

	#region Properties

	public SpeedyList<ICompletionData> Data { get; }

	public CompletionTrigger[] Keys { get; }

	public string Prefix { get; protected set; }

	#endregion

	#region Methods

	public static SpeedyList<ICompletionData> CreateList(IDispatcher dispatcher)
	{
		return new SpeedyList<ICompletionData>(dispatcher,
			new OrderBy<ICompletionData>(x => x.Priority),
			new OrderBy<ICompletionData>(x => x.DisplayText)
		)
		{
			FilterCheck = x => x.Priority < 100
		};
	}

	public abstract string GetTextToReplaceWithCompletionResults(string input, string completionText);

	/// <inheritdoc />
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

	/// <inheritdoc />
	public abstract bool TryGetAutoComplete(string input);

	#endregion
}

public interface ICompletionProvider
{
	#region Properties

	SpeedyList<ICompletionData> Data { get; }

	string Prefix { get; }

	#endregion

	#region Methods

	bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent);

	public bool TryGetAutoComplete(string input);

	#endregion
}