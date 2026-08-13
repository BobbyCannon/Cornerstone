#region References

using System;
using System.Threading;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Keystone.Messages;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Keystone;

[SourceReflection]
public class KeystoneBus : LifecycleTracker
{
	#region Constants

	public const int DefaultHistoryLimit = 100;

	#endregion

	#region Fields

	private long _historySequence;
	private bool _isHistoryEnabled;
	private string _historyFilterText;
	private BusHistoryFilter _historyFilter;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public KeystoneBus()
	{
		History = new PresentationList<ChannelMessageHistory> { Limit = DefaultHistoryLimit };
		_historyFilterText = string.Empty;
		_historyFilter = BusHistoryFilter.Parse(string.Empty);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Recent completed publishes when <see cref="IsHistoryEnabled" /> is true.
	/// Bounded by <see cref="PresentationList{T}.Limit" /> (default 100).
	/// </summary>
	public PresentationList<ChannelMessageHistory> History { get; }

	/// <summary>
	/// Live recording filter (text grammar). Empty means record all enabled traffic.
	/// Parsed once on set; applied in <see cref="OnMessageCompleted" /> before History.Add.
	/// See <see cref="BusHistoryFilter" /> (channel:, type:, error:).
	/// </summary>
	public string HistoryFilter
	{
		get => _historyFilterText;
		set
		{
			var text = value ?? string.Empty;
			if (_historyFilterText == text)
			{
				return;
			}

			_historyFilterText = text;
			_historyFilter = BusHistoryFilter.Parse(text);
		}
	}

	/// <summary>
	/// When true, records completed publishes into <see cref="History" /> (duration, handlers, errors).
	/// Default false: zero diagnostic cost on the publish path beyond the existing MessagePublished event.
	/// </summary>
	public bool IsHistoryEnabled
	{
		get => _isHistoryEnabled;
		set
		{
			if (_isHistoryEnabled == value)
			{
				return;
			}

			_isHistoryEnabled = value;
			SyncChannelHistorySubscriptions();
		}
	}

	public Action<Exception> OnError { get; set; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		foreach (var child in Children)
		{
			if (child is KeystoneChannel channel)
			{
				channel.ErrorOccurred += OnChannelError;
			}
		}

		SyncChannelHistorySubscriptions();
		base.InitializeLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		foreach (var child in Children)
		{
			if (child is KeystoneChannel channel)
			{
				channel.ErrorOccurred -= OnChannelError;
				channel.MessageCompleted -= OnMessageCompleted;
			}
		}

		base.UninitializeLifecycle();
	}

	private void OnChannelError(Exception exception)
	{
		OnError?.Invoke(exception);
	}

	private void OnMessageCompleted(ChannelMessagePublishResult result)
	{
		if (!_isHistoryEnabled)
		{
			return;
		}

		var filter = _historyFilter;
		if ((filter is not null) && !filter.IsMatchAll && !filter.Matches(result))
		{
			return;
		}

		var sequence = Interlocked.Increment(ref _historySequence);
		var name = result.Message?.GetType().Name;
		if (string.IsNullOrEmpty(name))
		{
			name = result.Type.ToString();
		}

		History.Add(new ChannelMessageHistory
		{
			Sequence = sequence,
			ChannelName = result.ChannelName,
			Name = name,
			Type = result.Type,
			PublishOn = DateTime.UtcNow,
			ElapsedTicks = result.ElapsedTicks,
			HandlerCount = result.HandlerCount,
			HadError = result.HadError,
			ErrorMessage = result.ErrorMessage
		});
	}

	private void SyncChannelHistorySubscriptions()
	{
		foreach (var child in Children)
		{
			if (child is not KeystoneChannel channel)
			{
				continue;
			}

			// Detach first so enable/disable is idempotent.
			channel.MessageCompleted -= OnMessageCompleted;
			if (_isHistoryEnabled)
			{
				channel.MessageCompleted += OnMessageCompleted;
			}
		}
	}

	#endregion
}