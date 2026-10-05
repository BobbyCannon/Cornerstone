#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public abstract partial class SecurityCardReader : Manager, IDispatchable
{
	#region Fields

	private readonly IDispatcher _dispatcher;

	#endregion

	#region Constructors

	protected SecurityCardReader(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
		IsAvailable = false;
		IsListening = false;
		MaximumDataToRead = 128;
		Card = new UnknownSecurityCard(dispatcher);
	}

	#endregion

	#region Properties

	[Notify]
	public partial SecurityCard Card { get; protected set; }

	[Notify]
	public partial bool IsAvailable { get; protected set; }

	[Notify]
	public partial bool IsListening { get; protected set; }

	[Notify]
	public partial int MaximumDataToRead { get; set; }

	#endregion

	#region Methods

	public void ClearCard()
	{
		Card?.ClearData();
		WriteCard();
	}

	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public abstract void ReadCard();

	[RelayCommand]
	public virtual Task RefreshAsync()
	{
		return Task.CompletedTask;
	}

	public abstract void StartListening();

	public abstract void StopListening();

	public abstract void WriteCard();

	protected virtual void OnCardInserted(SecurityCard e)
	{
		CardInserted?.Invoke(this, e);
	}

	protected virtual void OnCardRemoved(SecurityCard e)
	{
		CardRemoved?.Invoke(this, e);
	}

	protected virtual void RemoveCard()
	{
		var card = Card;
		Card = null;
		if (card != null)
		{
			OnCardRemoved(card);
		}
	}

	#endregion

	#region Events

	public event EventHandler<SecurityCard> CardInserted;
	public event EventHandler<SecurityCard> CardRemoved;

	#endregion
}
