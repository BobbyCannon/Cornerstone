#region References

using System;
using System.Drawing;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Represents the difference between two item positions,
/// providing a way to determine a  movement.
/// </summary>
[Serializable]
public readonly struct MovementDelta : IEquatable<MovementDelta>
{
	#region Fields

	/// <summary>
	/// A <see cref="MovementDelta" /> object that represents
	/// no item movement, having a relative position of 0, 0.
	/// </summary>
	/// <seealso cref="HasMoved" />
	public static readonly MovementDelta Zero = new(0f, 0f);

	/// <summary>
	/// Cached hash code for the current instance.
	/// </summary>
	private readonly int _hashCode;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new <see cref="MovementDelta" /> object that has the  specified delta coordinates.
	/// </summary>
	/// <param name="x"> Target position of the  X axis, relative to its source position. </param>
	/// <param name="y"> Target position of the  Y axis, relative to its source position. </param>
	public MovementDelta(float x, float y)
	{
		if (float.IsNaN(x))
		{
			throw new ArgumentException($"'{nameof(x)}' cannot be {float.NaN}.", nameof(x));
		}
		if (float.IsNaN(y))
		{
			throw new ArgumentException($"'{nameof(y)}' cannot be {float.NaN}.", nameof(y));
		}

		x = Math.Clamp(x, -2f, 2f);
		y = Math.Clamp(y, -2f, 2f);
		X = x;
		Y = y;

		if ((x == 0f) && (y == 0f))
		{
			Angle = 0f;
			Distance = 0f;
			Direction = MovementDirection.None;
		}
		else
		{
			InputMath.ConvertToPolar(X, Y, out var angle, out var distance);
			angle = InputMath.ConvertRadiansToNormal(angle);
			Angle = angle;
			Distance = distance;
			Direction = ConvertNormalAngleToJoystickDirection(Angle);
		}

		_hashCode = HashCode.Combine(X, Y);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the angle towards which the item has moved, relative to its source position.
	/// </summary>
	public float Angle { get; }

	/// <summary>
	/// Gets a <see cref="MovementDirection" /> constant that indicates the direction to which the item was moved.
	/// </summary>
	public MovementDirection Direction { get; }

	/// <summary>
	/// Gets the distance the item has moved, from its source position to its target position.
	/// </summary>
	public float Distance { get; }

	/// <summary>
	/// Gets a value that indicates movement, considering the delta.
	/// </summary>
	public bool HasMoved => (X != 0f) || (Y != 0f);

	/// <summary>
	/// Gets the target position of the  X axis, relative to
	/// its source position. This is by how much the  X axis
	/// has moved.
	/// </summary>
	public float X { get; }

	/// <summary>
	/// Gets the target position of the  Y axis, relative to
	/// its source position. This is by how much the  Y axis
	/// has moved.
	/// </summary>
	public float Y { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Converts the specified normalized angle to a
	/// <see cref="MovementDirection" /> constant that represents
	/// the direction of the angle.
	/// </summary>
	/// <param name="normalAngle">
	/// A value between 0 and 1, that
	/// specifies the normalized angle to convert.
	/// </param>
	/// <returns>
	/// A <see cref="MovementDirection" /> constant
	/// that specifies the direction of the angle.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// <paramref name="normalAngle" /> is <see cref="float.NaN" />.
	/// </exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MovementDirection ConvertNormalAngleToJoystickDirection(float normalAngle)
	{
		if (float.IsNaN(normalAngle))
		{
			throw new ArgumentException($"'{float.NaN}' is not a valid value for '{nameof(normalAngle)}' parameter.", nameof(normalAngle));
		}

		normalAngle = MathF.Abs(normalAngle);
		normalAngle -= MathF.Floor(normalAngle);

		if ((normalAngle >= 0.875f) || (normalAngle < 0.125f))
		{
			return MovementDirection.Up;
		}
		if ((normalAngle >= 0.125f) && (normalAngle < 0.375f))
		{
			return MovementDirection.Right;
		}
		if ((normalAngle >= 0.375f) && (normalAngle < 0.625f))
		{
			return MovementDirection.Down;
		}
		if ((normalAngle >= 0.625f) && (normalAngle < 0.875f))
		{
			return MovementDirection.Left;
		}
		return MovementDirection.None;
	}

	/// <summary>
	/// Determines if the current <see cref="MovementDelta" /> is
	/// identical to the specified <see cref="object" /> instance.
	/// Overrides <see cref="object.Equals(object?)" />
	/// </summary>
	/// <param name="obj">
	/// <see cref="object" /> instance to
	/// compare with the current <see cref="MovementDelta" />.
	/// </param>
	/// <returns>
	/// <see langword="true" /> if the current
	/// <see cref="MovementDelta" /> is identical to <paramref name="obj" />;
	/// otherwise, <see langword="false" />.
	/// </returns>
	/// <seealso cref="GetHashCode()" />
	public override bool Equals(object obj)
	{
		if (obj is MovementDelta joystickDelta)
		{
			return Equals(joystickDelta);
		}
		return false;
	}

	/// <summary>
	/// Determines if the current <see cref="MovementDelta" /> is
	/// identical to the specified <see cref="MovementDelta" />
	/// object.
	/// </summary>
	/// <param name="other">
	/// <see cref="MovementDelta" /> object to
	/// compare with the current <see cref="MovementDelta" />.
	/// </param>
	/// <returns>
	/// <see langword="true" /> if the current
	/// <see cref="MovementDelta" /> is identical to <paramref name="other" />;
	/// otherwise, <see langword="false" />.
	/// </returns>
	public bool Equals(MovementDelta other)
	{
		return (X == other.X) && (Y == other.Y);
	}

	/// <summary>
	/// Gets a <see cref="MovementDelta" /> object that represents the
	/// movement delta between the specified source and target
	/// positions of an item.
	/// </summary>
	/// <param name="source"> A <see cref="PointF" /> object representing the source item position. </param>
	/// <param name="target"> A <see cref="PointF" /> object representing the target item position. </param>
	/// <returns>
	/// A <see cref="MovementDelta" /> object that represents the difference between the specified source and target
	/// positions. If there is no difference between the source and target positions, <see cref="Zero" /> is returned.
	/// </returns>
	public static MovementDelta FromJoystickPosition(PointF source, PointF target)
	{
		return FromPosition(source.X, source.Y, target.X, target.Y);
	}

	/// <summary>
	/// Gets a <see cref="MovementDelta" /> object that represents the
	/// movement delta between the specified source and target
	/// positions of an item.
	/// </summary>
	/// <param name="sourceX"> A number between -1 and 1, representing the source position of the X axis of the item. </param>
	/// <param name="sourceY"> A number between -1 and 1, representing the source position of the Y axis of the item. </param>
	/// <param name="targetX"> A number between -1 and 1, representing the target position of the X axis of the item. </param>
	/// <param name="targetY"> A number between -1 and 1, representing the target position of the Y axis of the item. </param>
	/// <returns>
	/// A <see cref="MovementDelta" /> object that represents
	/// the difference between the specified source and target
	/// positions. If there is no difference between the source and
	/// target positions, <see cref="Zero" /> is returned.
	/// </returns>
	public static MovementDelta FromPosition(float sourceX, float sourceY, float targetX, float targetY)
	{
		// Validate parameters.
		if (float.IsNaN(sourceX))
		{
			throw new ArgumentException($"'{nameof(sourceX)}' cannot be {float.NaN}", nameof(sourceX));
		}
		if (float.IsNaN(sourceY))
		{
			throw new ArgumentException($"'{nameof(sourceY)}' cannot be {float.NaN}", nameof(sourceY));
		}
		if (float.IsNaN(targetX))
		{
			throw new ArgumentException($"'{nameof(targetX)}' cannot be {float.NaN}", nameof(targetX));
		}
		if (float.IsNaN(targetY))
		{
			throw new ArgumentException($"'{nameof(targetY)}' cannot be {float.NaN}", nameof(targetY));
		}

		sourceX = InputMath.Clamp11(sourceX);
		sourceY = InputMath.Clamp11(sourceY);
		targetX = InputMath.Clamp11(targetX);
		targetY = InputMath.Clamp11(targetY);

		// Return a delta object with the relative coordinates.
		if ((sourceX == targetX) && (sourceY == targetY))
		{
			return Zero;
		}
		return new MovementDelta(targetX - sourceX, targetY - sourceY);
	}

	/// <summary>
	/// Gets the hash code for the current <see cref="MovementDelta" />
	/// object. Overrides <see cref="object.GetHashCode()" />.
	/// </summary>
	/// <returns> The computed hash code. </returns>
	public override int GetHashCode()
	{
		return _hashCode;
	}

	/// <summary>
	/// Gets the source position of the item, considering its specified
	/// target position.
	/// </summary>
	/// <param name="targetX"> Target position of the  X axis. </param>
	/// <param name="targetY"> Target position of the  Y axis. </param>
	/// <returns>
	/// A <see cref="PointF" /> object that represents the
	/// source item position.
	/// </returns>
	public PointF GetSourcePosition(float targetX, float targetY)
	{
		if (float.IsNaN(targetX))
		{
			throw new ArgumentException($"'{nameof(targetX)}' cannot be {float.NaN}.", nameof(targetX));
		}
		if (float.IsNaN(targetY))
		{
			throw new ArgumentException($"'{nameof(targetY)}' cannot be {float.NaN}.", nameof(targetY));
		}

		targetX = InputMath.Clamp11(targetX);
		targetY = InputMath.Clamp11(targetY);
		return new PointF(targetX - X, targetY - Y);
	}

	/// <summary>
	/// Gets the source position of the item, considering its specified target position.
	/// </summary>
	/// <param name="item"> <see cref="PointF" /> object representing the target item position. </param>
	public PointF GetSourcePosition(PointF item)
	{
		return GetSourcePosition(item.X, item.Y);
	}

	/// <summary>
	/// Gets the target position of the item, considering its specified source position.
	/// </summary>
	/// <param name="sourceX"> Source position of the  X axis. </param>
	/// <param name="sourceY"> Source position of the  Y axis. </param>
	/// <returns> A <see cref="PointF" /> object that represents the target item position. </returns>
	public PointF GetTargetPosition(float sourceX, float sourceY)
	{
		if (float.IsNaN(sourceX))
		{
			throw new ArgumentException($"'{nameof(sourceX)}' cannot be {float.NaN}.", nameof(sourceX));
		}
		if (float.IsNaN(sourceY))
		{
			throw new ArgumentException($"'{nameof(sourceY)}' cannot be {float.NaN}.", nameof(sourceY));
		}

		sourceX = InputMath.Clamp11(sourceX);
		sourceY = InputMath.Clamp11(sourceY);
		return new PointF(sourceX + X, sourceY + Y);
	}

	/// <summary>
	/// Gets the target position of the item, considering its specified source position.
	/// </summary>
	/// <param name="item"> <see cref="PointF" /> object representing the source item position. </param>
	public PointF GetTargetPosition(PointF item)
	{
		return GetTargetPosition(item.X, item.Y);
	}

	/// <summary>
	/// Determines if both <see cref="MovementDelta" /> objects are identical.
	/// </summary>
	/// <param name="left"> Left <see cref="MovementDelta" /> operand. </param>
	/// <param name="right"> Right <see cref="MovementDelta" /> operand. </param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="left" /> is identical
	/// to <paramref name="right" />; otherwise, <see langword="false" />.
	/// </returns>
	public static bool operator ==(MovementDelta left, MovementDelta right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Determines if both <see cref="MovementDelta" /> objects differ.
	/// </summary>
	/// <param name="left"> Left <see cref="MovementDelta" /> operand. </param>
	/// <param name="right"> Right <see cref="MovementDelta" /> operand. </param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="left" /> differs from
	/// <paramref name="right" />; otherwise, <see langword="false" />.
	/// </returns>
	public static bool operator !=(MovementDelta left, MovementDelta right)
	{
		return !left.Equals(right);
	}

	/// <summary>
	/// Gets the <see cref="string" /> representation of the <see cref="MovementDelta" /> instance.
	/// </summary>
	/// <returns> The <see cref="string" /> representation of the <see cref="MovementDelta" />. </returns>
	public override string ToString()
	{
		return $"{X}, {Y}";
	}

	#endregion
}