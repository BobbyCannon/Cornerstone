#region References

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Cornerstone.Compare;
using Cornerstone.Text;
using Cornerstone.Text.Buffers;

#endregion

namespace Cornerstone.Serialization.Consumer;

/// <inheritdoc cref="IObjectConsumer" />
public abstract class ObjectConsumer<T>
	: TextBuilder<T>, IObjectConsumer, IReferenceTracker
	where T : ISerializationSettings
{
	#region Fields

	private readonly ConcurrentQueue<ObjectConsumerMode> _consumerModes;
	private readonly IReferenceTracker _referenceTracker;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the serializer consumer.
	/// </summary>
	/// <param name="settings"> The settings. </param>
	protected ObjectConsumer(T settings) : this(null, settings)
	{
	}

	/// <summary>
	/// Initialize the serializer consumer.
	/// </summary>
	/// <param name="buffer"> The buffer to write to. </param>
	/// <param name="settings"> The settings. </param>
	protected ObjectConsumer(IStringBuffer buffer, T settings) : base(buffer, settings)
	{
		_consumerModes = new ConcurrentQueue<ObjectConsumerMode>();
		_referenceTracker = new ReferenceTracker();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The current mode of the consumer.
	/// </summary>
	public ObjectConsumerMode Mode { get; private set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public void AddReference(object value)
	{
		_referenceTracker.AddReference(value);
	}

	/// <inheritdoc />
	public bool AlreadyProcessed(object value)
	{
		return _referenceTracker.AlreadyProcessed(value);
	}

	/// <inheritdoc />
	public abstract IObjectConsumer Boolean(bool value);

	/// <inheritdoc />
	public abstract IObjectConsumer CompleteObject();

	/// <inheritdoc />
	public abstract IObjectConsumer Null();

	/// <inheritdoc />
	public abstract IObjectConsumer Number(object value);

	/// <inheritdoc />
	public void RemoveReference(object value)
	{
		_referenceTracker.RemoveReference(value);
	}

	/// <summary>
	/// Reset the consumer.
	/// </summary>
	/// <returns> The consumer after reset. </returns>
	public virtual IObjectConsumer Reset()
	{
		// _consumerModes.Clear();
		_referenceTracker.Clear();
		return this;
	}

	/// <inheritdoc />
	public abstract IObjectConsumer StartObject(Type type);

	/// <inheritdoc />
	public abstract IObjectConsumer String(string value);

	/// <inheritdoc />
	public abstract IObjectConsumer WriteObject(object value);

	/// <inheritdoc />
	public abstract IObjectConsumer WriteProperty(string name, object value);

	/// <inheritdoc />
	public virtual IObjectConsumer WriteProperty(PropertyInfo info, object value)
	{
		return WriteProperty(info.Name, value);
	}

	/// <inheritdoc />
	public abstract IObjectConsumer WriteRawString(string value);

	/// <summary>
	/// Pop the consumer mode.
	/// </summary>
	protected void PopConsumerMode()
	{
		Mode = _consumerModes.TryDequeue(out var mode) ? mode : ObjectConsumerMode.Unknown;
	}

	/// <summary>
	/// Push the consumer mode.
	/// </summary>
	/// <param name="mode"> The mode to push. </param>
	protected void PushConsumerMode(ObjectConsumerMode mode)
	{
		Mode = mode;

		_consumerModes.Enqueue(mode);
	}

	#endregion
}

/// <summary>
/// This interface is used to convert from one representation of the data to another.
/// </summary>
public interface IObjectConsumer
{
	#region Methods

	/// <summary>
	/// Consumes a boolean value.
	/// </summary>
	/// <param name="value"> The bool value. </param>
	IObjectConsumer Boolean(bool value);

	/// <summary>
	/// Complete the object.
	/// </summary>
	IObjectConsumer CompleteObject();

	/// <summary>
	/// Consumes a null value.
	/// </summary>
	IObjectConsumer Null();

	/// <summary>
	/// Consumes a number value.
	/// </summary>
	/// <param name="value"> The number value. </param>
	IObjectConsumer Number(object value);

	/// <summary>
	/// Start consuming an object.
	/// </summary>
	/// <param name="type"> The type of the object. </param>
	/// <returns>
	/// A consumer which is used to assign properties to the object.
	/// </returns>
	IObjectConsumer StartObject(Type type);

	/// <summary>
	/// Write a string to the consumer. The value should already be in proper format for the consumer.
	/// </summary>
	/// <param name="value"> The string value. </param>
	IObjectConsumer String(string value);

	/// <summary>
	/// Write an object of non-specific type to the consumer.
	/// </summary>
	/// <param name="value"> The value to write. </param>
	/// <returns> </returns>
	IObjectConsumer WriteObject(object value);

	/// <summary>
	/// Write the property to the consumer.
	/// </summary>
	/// <param name="name"> The property name. </param>
	/// <param name="value"> The value to write. </param>
	IObjectConsumer WriteProperty(string name, object value);

	/// <summary>
	/// Write the property to the consumer.
	/// </summary>
	/// <param name="info"> The property info. </param>
	/// <param name="value"> The value to write. </param>
	IObjectConsumer WriteProperty(PropertyInfo info, object value);

	/// <summary>
	/// Format then write a string to the consumer. This method will escape and wrap the string with quotes.
	/// </summary>
	/// <param name="value"> The string value. </param>
	IObjectConsumer WriteRawString(string value);

	#endregion
}