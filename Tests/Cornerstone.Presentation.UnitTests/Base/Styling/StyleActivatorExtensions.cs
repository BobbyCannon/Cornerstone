#region References

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Styling.Activators;
using Observable = Cornerstone.Presentation.Reactive.Observable;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

internal static class StyleActivatorExtensions
{
	#region Methods

	public static IDisposable Subscribe(this IStyleActivator activator, Action<bool> action)
	{
		return Observable.Subscribe(activator.ToObservable(), action);
	}

	public static async Task<bool> Take(this IStyleActivator activator, int value)
	{
		return await System.Reactive.Linq.Observable.Take(activator.ToObservable(), value);
	}

	public static IObservable<bool> ToObservable(this IStyleActivator activator)
	{
		if (activator == null)
		{
			throw new ArgumentNullException(nameof(activator));
		}

		return new ObservableAdapter(activator);
	}

	#endregion

	#region Classes

	private class ObservableAdapter : LightweightObservableBase<bool>, IStyleActivatorSink
	{
		#region Fields

		private readonly IStyleActivator _source;

		#endregion

		#region Constructors

		public ObservableAdapter(IStyleActivator source)
		{
			_source = source;
		}

		#endregion

		#region Methods

		protected override void Deinitialize()
		{
			_source.Unsubscribe(this);
		}

		protected override void Initialize()
		{
			_source.Subscribe(this);
		}

		protected override void Subscribed(IObserver<bool> observer, bool first)
		{
			observer.OnNext(_source.GetIsActive());
		}

		void IStyleActivatorSink.OnNext(bool value)
		{
			PublishNext(value);
		}

		#endregion
	}

	#endregion
}