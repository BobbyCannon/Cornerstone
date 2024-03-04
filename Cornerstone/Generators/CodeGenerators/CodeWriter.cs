#region References

using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators.CodeGenerators;

/// <inheritdoc cref="ICodeWriter" />
public abstract class CodeWriter<T> : ObjectConsumer<T>, ICodeWriter
	where T : ICodeWriterOptions
{
	#region Constructors

	/// <summary>
	/// Initialize the code writer.
	/// </summary>
	protected CodeWriter(T settings) : base(settings)
	{
	}

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

		var lastIndex = properties.Count - 1;

		for (var index = 0; index < properties.Count; index++)
		{
			var property = properties[index];
			WriteProperty(property, property.GetValue(value));

			if (index != lastIndex)
			{
				WriteRawString(",");
			}

			NewLine();
		}

		CompleteObject();
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer WriteRawString(string value)
	{
		Append(value);
		return this;
	}

	#endregion
}

/// <summary>
/// Represents a code writer.
/// </summary>
public interface ICodeWriter : ITextBuilder
{
	#region Methods

	/// <summary>
	/// Write the value.
	/// </summary>
	/// <param name="value"> The value to append to the code writer. </param>
	void AppendObject(object value);

	#endregion
}