#region References

using System;
using System.ComponentModel;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.Text.History;

public class CommandHistoryProvider : Presentation.PresentationList<CommandHistory>, ICommandHistoryProvider
{
	#region Fields

	private int _index;

	#endregion

	#region Constructors

	public CommandHistoryProvider()
	{
		_index = 0;
	}

	#endregion

	#region Properties

	public bool HasHistory => Count > 0;

	#endregion

	#region Methods

	public void Append(string command)
	{
		var last = LastOrDefault();
		if ((last != null) && string.Equals(last.Command, command, StringComparison.OrdinalIgnoreCase))
		{
			last.Count++;
		}
		else
		{
			Add(new CommandHistory(command));
		}

		_index = Count;
	}

	public override void Clear()
	{
		base.Clear();
		_index = 0;
	}

	public string Next()
	{
		if (Count <= 0)
		{
			return null;
		}

		if (_index >= Count)
		{
			return null;
		}

		_index++;

		if (_index >= Count)
		{
			return null;
		}

		return base[_index].Command;
	}

	public string Previous()
	{
		if (Count <= 0)
		{
			return null;
		}

		if (_index <= 0)
		{
			_index = 0;
			return null;
		}

		_index--;

		return base[_index].Command;
	}

	public virtual void Reset()
	{
		_index = Count;
	}

	protected override void OnListUpdated(PresentationListUpdatedEventArg<CommandHistory> e)
	{
		if (_index > Count)
		{
			_index = Count;
		}

		NotifyComputedPropertyChanged(nameof(HasHistory));
		base.OnListUpdated(e);
	}

	#endregion
}

public interface ICommandHistoryProvider : IPresentationList<CommandHistory>, INotifyPropertyChanged
{
	#region Properties

	bool HasHistory { get; }

	#endregion

	#region Methods

	void Append(string command);

	string Next();

	string Previous();

	void Reset();

	#endregion
}