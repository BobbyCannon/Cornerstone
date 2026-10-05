#region References

using System;
using System.ComponentModel;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Defines the priorities with which jobs can be invoked on a dispatcher.
/// </summary>
public readonly struct DispatcherPriority : IEquatable<DispatcherPriority>, IComparable<DispatcherPriority>
{
	#region Constructors

	private DispatcherPriority(int value)
	{
		Value = value;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The integer value of the priority.
	/// </summary>
	public int Value { get; }

	#endregion

	#region Fields

	/// <summary>
	/// The lowest foreground dispatcher priority.
	/// </summary>
	public static readonly DispatcherPriority Default = new(0);

	internal static readonly DispatcherPriority MinimumForegroundPriority = Default;

	/// <summary>
	/// The job will be processed with the same priority as input.
	/// </summary>
	public static readonly DispatcherPriority Input = new(Default - 1);

	/// <summary>
	/// The job will be processed after other non-idle operations have completed.
	/// </summary>
	public static readonly DispatcherPriority Background = new(Input - 1);

	/// <summary>
	/// The job will be processed after background operations have completed.
	/// </summary>
	public static readonly DispatcherPriority ContextIdle = new(Background - 1);

	/// <summary>
	/// The job will be processed when the application is idle.
	/// </summary>
	public static readonly DispatcherPriority ApplicationIdle = new(ContextIdle - 1);

	/// <summary>
	/// The job will be processed when the system is idle.
	/// </summary>
	public static readonly DispatcherPriority SystemIdle = new(ApplicationIdle - 1);

	/// <summary>
	/// Minimum possible priority that's actually dispatched.
	/// </summary>
	internal static readonly DispatcherPriority MinimumActiveValue = new(SystemIdle);

	/// <summary>
	/// A dispatcher priority for jobs that shouldn't be executed yet.
	/// </summary>
	public static readonly DispatcherPriority Inactive = new(MinimumActiveValue - 1);

	/// <summary>
	/// Minimum valid priority.
	/// </summary>
	internal static readonly DispatcherPriority MinValue = new(Inactive);

	/// <summary>
	/// Used internally in dispatcher code.
	/// </summary>
	public static readonly DispatcherPriority Invalid = new(MinimumActiveValue - 2);

	/// <summary>
	/// The job will be processed after layout and render but before input.
	/// </summary>
	public static readonly DispatcherPriority Loaded = new(Default + 1);

	/// <summary>
	/// A special priority for platforms with a UI render timer or for forced full rasterization requests.
	/// </summary>
	public static readonly DispatcherPriority UiThreadRender = new(Loaded + 1);

	/// <summary>
	/// Synchronize native control host positions, IME, and similar after render.
	/// </summary>
	internal static readonly DispatcherPriority AfterRender = new(UiThreadRender + 1);

	/// <summary>
	/// The job will be processed with the same priority as render.
	/// </summary>
	public static readonly DispatcherPriority Render = new(AfterRender + 1);

	/// <summary>
	/// A special platform hook for jobs to be executed before the normal render cycle.
	/// </summary>
	public static readonly DispatcherPriority BeforeRender = new(Render + 1);

	/// <summary>
	/// A special priority for platforms that resize the render target asynchronously.
	/// </summary>
	public static readonly DispatcherPriority AsyncRenderTargetResize = new(BeforeRender + 1);

	/// <summary>
	/// The job will be processed with the same priority as data binding.
	/// </summary>
	[Obsolete("WPF compatibility")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly DispatcherPriority DataBind = new(AsyncRenderTargetResize + 1);

	/// <summary>
	/// The job will be processed with normal priority.
	/// </summary>
#pragma warning disable CS0618
	public static readonly DispatcherPriority Normal = new(DataBind + 1);
#pragma warning restore CS0618

	/// <summary>
	/// The job will be processed before other asynchronous operations.
	/// </summary>
	public static readonly DispatcherPriority Send = new(Normal + 1);

	/// <summary>
	/// Maximum possible priority.
	/// </summary>
	public static readonly DispatcherPriority MaxValue = Send;

	#endregion

	#region Methods

	public static DispatcherPriority FromValue(int value)
	{
		if ((value < MinValue.Value) || (value > MaxValue.Value))
		{
			throw new ArgumentOutOfRangeException(nameof(value));
		}

		return new DispatcherPriority(value);
	}

	public static implicit operator int(DispatcherPriority priority)
	{
		return priority.Value;
	}

	public static implicit operator DispatcherPriority(int value)
	{
		return FromValue(value);
	}

	/// <inheritdoc />
	public bool Equals(DispatcherPriority other)
	{
		return Value == other.Value;
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		return obj is DispatcherPriority other && Equals(other);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public static bool operator ==(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value != right.Value;
	}

	public static bool operator <(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value < right.Value;
	}

	public static bool operator >(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value > right.Value;
	}

	public static bool operator <=(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value <= right.Value;
	}

	public static bool operator >=(DispatcherPriority left, DispatcherPriority right)
	{
		return left.Value >= right.Value;
	}

	/// <inheritdoc />
	public int CompareTo(DispatcherPriority other)
	{
		return Value.CompareTo(other.Value);
	}

	public static void Validate(DispatcherPriority priority, string parameterName)
	{
		if ((priority < Inactive) || (priority > MaxValue))
		{
			throw new ArgumentException("Invalid DispatcherPriority value", parameterName);
		}
	}

#pragma warning disable CS0618
	/// <inheritdoc />
	public override string ToString()
	{
		if (this == Invalid)
		{
			return nameof(Invalid);
		}
		if (this == Inactive)
		{
			return nameof(Inactive);
		}
		if (this == SystemIdle)
		{
			return nameof(SystemIdle);
		}
		if (this == ContextIdle)
		{
			return nameof(ContextIdle);
		}
		if (this == ApplicationIdle)
		{
			return nameof(ApplicationIdle);
		}
		if (this == Background)
		{
			return nameof(Background);
		}
		if (this == Input)
		{
			return nameof(Input);
		}
		if (this == Default)
		{
			return nameof(Default);
		}
		if (this == Loaded)
		{
			return nameof(Loaded);
		}
		if (this == UiThreadRender)
		{
			return nameof(UiThreadRender);
		}
		if (this == AfterRender)
		{
			return nameof(AfterRender);
		}
		if (this == Render)
		{
			return nameof(Render);
		}
		if (this == BeforeRender)
		{
			return nameof(BeforeRender);
		}
		if (this == AsyncRenderTargetResize)
		{
			return nameof(AsyncRenderTargetResize);
		}
		if (this == DataBind)
		{
			return nameof(DataBind);
		}
		if (this == Normal)
		{
			return nameof(Normal);
		}
		if (this == Send)
		{
			return nameof(Send);
		}

		return Value.ToString();
	}
#pragma warning restore CS0618

	#endregion
}
