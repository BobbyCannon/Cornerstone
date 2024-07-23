#region References

using System;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// Represents a value for a tracker path.
/// </summary>
public class TrackerPathValue : Notifiable, IEquatable<TrackerPathValue>, ITrackerPathValue
{
	#region Constructors

	/// <summary>
	/// Instantiates a new instance of the class.
	/// </summary>
	public TrackerPathValue()
	{
	}

	/// <summary>
	/// Instantiates a new instance of the class.
	/// </summary>
	public TrackerPathValue(string name, object value)
	{
		if (name == null)
		{
			throw new ArgumentNullException(nameof(name), "The name cannot be null.");
		}

		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("The name is required.", nameof(name));
		}

		Name = name;
		Value = value?.ToString() ?? string.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the value.
	/// </summary>
	public string Value { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public bool Equals(TrackerPathValue other)
	{
		if (ReferenceEquals(null, other))
		{
			return false;
		}
		if (ReferenceEquals(this, other))
		{
			return true;
		}
		return (Name == other.Name) && (Value == other.Value);
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
		{
			return false;
		}
		if (ReferenceEquals(this, obj))
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((TrackerPathValue) obj);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		unchecked
		{
			return ((Name != null ? Name.GetHashCode() : 0) * 397)
				^ (Value != null ? Value.GetHashCode() : 0);
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{Name}:{Value}";
	}

	#endregion
}

/// <summary>
/// Represents a value for a tracker path.
/// </summary>

public interface ITrackerPathValue
{
	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	string Name { get; set; }

	/// <summary>
	/// Gets or sets the value.
	/// </summary>
	string Value { get; set; }
}