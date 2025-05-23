﻿#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators.CodeGenerators;

/// <inheritdoc cref="ICodeWriter" />
public abstract class CodeWriter<T> : ObjectConsumer<T>, ICodeWriter
	where T : ICodeWriterSettings
{
	#region Constructors

	/// <summary>
	/// Initialize the code writer.
	/// </summary>
	protected CodeWriter(T settings) : base(settings)
	{
	}

	#endregion

	#region Properties

	public bool WritingPropertyValue { get; protected set; }

	#endregion

	#region Methods

	/// <summary>
	/// Converts the object to code.
	/// </summary>
	/// <param name="value"> The value object to write. </param>
	public virtual void AppendObject(object value)
	{
		switch (value)
		{
			case null:
			{
				Null();
				return;
			}
			case bool bValue:
			{
				Boolean(bValue);
				return;
			}
			case string sValue:
			{
				base.Append($"\"{sValue.Escape()}\"");
				return;
			}
		}

		// See Code Generators for custom types

		WriteObject(value);
	}

	/// <inheritdoc />
	public override IObjectConsumer Boolean(bool value)
	{
		Append(value ? "true" : "false");
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer CompleteObject()
	{
		PopIndent();

		switch (Mode)
		{
			case ObjectConsumerMode.Array:
			{
				Append("]");
				break;
			}
			case ObjectConsumerMode.Object:
			{
				Append("}");
				break;
			}
		}

		PopConsumerMode();
		return this;
	}

	/// <summary>
	/// Get generator for value.
	/// </summary>
	/// <param name="value"> The value to get a generator for. </param>
	/// <returns> The code generator or null if none available. </returns>
	public abstract ICodeGenerator GetGenerator(object value);

	/// <inheritdoc />
	public TValue GetSettingsValue<TValue>(Func<ICodeWriterSettings, TValue> predicate)
	{
		return predicate.Invoke(Settings);
	}

	/// <inheritdoc />
	public override IObjectConsumer Null()
	{
		Append("null");
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer Number(object value)
	{
		return this;
	}

	/// <summary>
	/// Reset the code consumer.
	/// </summary>
	public override IObjectConsumer Reset()
	{
		Clear();
		return base.Reset();
	}

	/// <inheritdoc />
	public override IObjectConsumer String(string value)
	{
		return this;
	}

	public void UpdateSettings(Action<T> update)
	{
		update?.Invoke(Settings);
	}

	/// <summary>
	/// Converts the object to code.
	/// </summary>
	/// <param name="value"> The value object to write. </param>
	public override IObjectConsumer WriteObject(object value)
	{
		if (value is IObjectCodeWriter objectCodeWriter)
		{
			objectCodeWriter.Write(this);
			return this;
		}

		var generator = GetGenerator(value);
		if (generator != null)
		{
			generator.AppendCode(this, value.GetType(), value);
			return this;
		}

		var valueType = value.GetType();
		var properties = valueType.GetCachedProperties();

		StartObject(valueType);

		var hasProperties = false;

		for (var index = 0; index < properties.Count; index++)
		{
			var propertyInfo = properties[index];
			if (propertyInfo.IsIndexer())
			{
				// Property is an indexer
				continue;
			}

			var propertyValue = propertyInfo.GetValue(value);

			if (Settings.IgnoreDefaultValues
				&& ((propertyValue?.IsDefaultValue() == true)
					|| (propertyInfo.PropertyType.IsNullable()
						&& (propertyValue == null))))
			{
				continue;
			}
			if (Settings.IgnoreNullValues
				&& (propertyValue == null))
			{
				continue;
			}
			if (Settings.IgnoreReadOnly
				&& !propertyInfo.CanWrite)
			{
				continue;
			}

			if (hasProperties)
			{
				WriteRawString(",");
				NewLine();
			}

			WriteProperty(propertyInfo, propertyValue);
			hasProperties = true;
		}

		NewLine();

		CompleteObject();
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer WriteRawString(string value)
	{
		Append(value);
		return this;
	}

	protected void WritePropertyValue(object value)
	{
		WritingPropertyValue = true;
		AppendObject(value);
		WritingPropertyValue = false;
	}

	/// <inheritdoc />
	void ICodeWriter.UpdateSettings(Action<ICodeWriterSettings> update)
	{
		UpdateSettings(x => update(x));
	}

	#endregion
}

/// <summary>
/// Represents a code writer.
/// </summary>
public interface ICodeWriter : ITextBuilder
{
	#region Properties

	/// <summary>
	/// True if a property value is being written.
	/// </summary>
	bool WritingPropertyValue { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Write the value.
	/// </summary>
	/// <param name="value"> The value to append to the code writer. </param>
	void AppendObject(object value);

	/// <summary>
	/// Get a settings value.
	/// </summary>
	/// <param name="predicate"> The predicate for settings. </param>
	T GetSettingsValue<T>(Func<ICodeWriterSettings, T> predicate);

	/// <summary>
	/// Updates settings.
	/// </summary>
	/// <param name="update"> The update for settings. </param>
	void UpdateSettings(Action<ICodeWriterSettings> update);

	#endregion
}