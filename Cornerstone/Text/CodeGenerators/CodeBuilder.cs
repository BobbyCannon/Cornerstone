#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Text.CodeGenerators.TypeGenerators;

#endregion

namespace Cornerstone.Text.CodeGenerators;

public class CodeBuilder : TextWriter
{
	#region Fields

	private static readonly IList<CodeGenerator> _builtInGenerators;
	private readonly Stack<CodeBuilderMode> _consumerModes;
	private static readonly List<Func<SourceTypeInfo, string, object, string>> _propertyValueProviders;
	private static readonly ReadOnlyDictionary<Type, string> _simplifiedTypeNames;

	#endregion

	#region Constructors

	public CodeBuilder()
		: this(new StringBuffer(), new CodeBuilderSettings())
	{
	}

	public CodeBuilder(int length)
		: this(new StringBuffer(length), new CodeBuilderSettings())
	{
	}

	public CodeBuilder(IStringBuffer buffer)
		: this(buffer, new CodeBuilderSettings())
	{
	}

	public CodeBuilder(IStringBuffer buffer, CodeBuilderSettings settings)
		: base(buffer, settings)
	{
		_consumerModes = new Stack<CodeBuilderMode>();

		Mode = CodeBuilderMode.Unknown;
		Settings = settings;
	}

	static CodeBuilder()
	{
		_simplifiedTypeNames = new ReadOnlyDictionary<Type, string>(
			new Dictionary<Type, string>
			{
				{ typeof(char), "char" },
				{ typeof(bool), "bool" },
				{ typeof(byte), "byte" },
				{ typeof(sbyte), "sbyte" },
				{ typeof(short), "short" },
				{ typeof(ushort), "ushort" },
				{ typeof(int), "int" },
				{ typeof(uint), "uint" },
				{ typeof(long), "long" },
				{ typeof(ulong), "ulong" },
				{ typeof(decimal), "decimal" },
				{ typeof(float), "float" },
				{ typeof(double), "double" },
				{ typeof(string), "string" }
			}
		);

		_builtInGenerators = new List<CodeGenerator>
		{
			new StringCodeGenerator(),
			new EnumerableCodeGenerator(),
			new SystemDrawingCodeGenerator(),
			new NumberCodeGenerator(),
			new TimeCodeGenerator(),
			new EnumCodeGenerator(),
			new DateCodeGenerator(),
			new GuidCodeGenerator(),
			new VersionCodeGenerator(),
			new FuncCodeGenerator()
		};

		_propertyValueProviders = [];
	}

	#endregion

	#region Properties

	public CodeBuilderMode Mode { get; private set; }

	public override CodeBuilderSettings Settings { get; }

	#endregion

	#region Methods

	public static string AccessibilityToString(SourceAccessibility accessibility)
	{
		return accessibility switch
		{
			SourceAccessibility.None => string.Empty,
			SourceAccessibility.Internal => "internal",
			SourceAccessibility.Public => "public",
			SourceAccessibility.Private => "private",
			SourceAccessibility.ProtectedAndInternal => "private protected",
			SourceAccessibility.Protected => "protected",
			SourceAccessibility.ProtectedOrInternal => "protected internal",
			_ => throw new ArgumentException("Unknown member", nameof(accessibility))
		};
	}

	public override void Clear()
	{
		Buffer.Clear();
		base.Clear();
	}

	/// <summary>
	/// Converts data type to the code simplified type. Ex. Int16 to short, Single to float
	/// </summary>
	/// <param name="type"> The type to get the simplified name for. </param>
	/// <returns> The simplified name if found otherwise the Type.Name. </returns>
	public static string GetCodeTypeName(Type type)
	{
		if (_simplifiedTypeNames.TryGetValue(type, out var value))
		{
			var isNullableType = type.IsNullableType();
			return isNullableType ? $"{value}?" : value;
		}

		if (type.ImplementsType(typeof(Nullable<>)))
		{
			var baseType = type.FromNullableType();
			var name = GetCodeTypeName(baseType);
			return name + "?";
		}

		if (!type.IsGenericType)
		{
			return type.Name;
		}

		var typeName = type.Name;
		var index = typeName.IndexOf('`');
		if (index >= 0)
		{
			typeName = typeName.Substring(0, index);
		}

		var genericArguments = type.GetGenericArguments();
		return $"{typeName}<{string.Join(", ", genericArguments.Select(GetCodeTypeName))}>";
	}

	public static void RegisterPropertyValueProvider(Func<SourceTypeInfo, string, object, string> provider)
	{
		_propertyValueProviders.Add(provider);
	}

	public static string ToCSharp(object value, Action<CodeBuilderSettings> update = null)
	{
		var writer = new CodeBuilder();
		update?.Invoke(writer.Settings);
		writer.WriteObject(value);
		return writer.ToString();
	}

	public override string ToString()
	{
		return Buffer.ToString();
	}

	public bool TryAppendLiteral(object value)
	{
		if (value == null)
		{
			Append("null");
			return true;
		}
		if (value is bool b)
		{
			Append(b ? "true" : "false");
			return true;
		}
		if (value is string s)
		{
			Append($"\"{s}\"");
			return true;
		}
		if (value is float f)
		{
			Append(f.ToString("G9"));
			return true;
		}
		if (value is double d)
		{
			Append(d.ToString("G17"));
			return true;
		}

		return false;
	}

	/// <summary>
	/// Try to detect the starting indent of the content provided.
	/// </summary>
	/// <param name="content"> The content to test. </param>
	public void TryDetectIndent(string content)
	{
		var c = content.FirstOrDefault(x => x is '\t' or ' ');
		if (c == 0)
		{
			Settings.IndentChar = '\t';
			Settings.IndentLength = 1;
			Indent = 0;
			return;
		}

		var currentIndentCount = CountWhile(content, x => x == c);
		Settings.IndentChar = c;
		Settings.IndentLength = (uint) (c == '\t' ? 1 : 4);
		Indent = (uint) (currentIndentCount / Settings.IndentLength);
	}

	public void WriteObject<T>(T actual)
	{
		var sourceInfoType = SourceReflector.GetSourceType(actual?.GetType() ?? typeof(T));
		var generator = _builtInGenerators.FirstOrDefault(x => x.SupportsType(sourceInfoType.Type));
		if (generator != null)
		{
			generator.WriteObject(this, sourceInfoType, actual);
			return;
		}

		WriteObject(sourceInfoType, actual);
	}

	public void WriteProperty(SourceTypeInfo typeInfo, SourcePropertyInfo propertyInfo, object value)
	{
		PushConsumerMode(CodeBuilderMode.Property);

		try
		{
			switch (Settings.DesiredOutput)
			{
				case CodeBuilderOutput.Declaration:
				{
					Append(AccessibilityToString(propertyInfo.Accessibility));
					Append(" ");
					Append(GetCodeTypeName(propertyInfo.PropertyInfo.PropertyType));
					Append(" ");
					Append(propertyInfo.Name);
					Append(" {");
					if (propertyInfo.CanRead)
					{
						if ((propertyInfo.AccessibilityForGet != SourceAccessibility.None)
							&& (propertyInfo.AccessibilityForGet != propertyInfo.Accessibility))
						{
							Append(" ");
							Append(AccessibilityToString(propertyInfo.AccessibilityForGet));
						}
						Append(" get;");
					}
					if (propertyInfo.CanWrite)
					{
						if ((propertyInfo.AccessibilityForSet != SourceAccessibility.None)
							&& (propertyInfo.AccessibilityForSet != propertyInfo.Accessibility))
						{
							Append(" ");
							Append(AccessibilityToString(propertyInfo.AccessibilityForSet));
						}
						Append(" set;");
					}
					Append(" }");
					break;
				}
				default:
				{
					Append(propertyInfo.Name);
					Append(" = ");

					var rawValue = propertyInfo.GetValue(value);
					var customCode = TryGetCustomCodeValue(typeInfo, propertyInfo, rawValue);

					if (customCode != null)
					{
						Append(customCode);
					}
					else
					{
						WriteObject(rawValue);
					}
					break;
				}
			}
		}
		finally
		{
			PopConsumerMode();
		}
	}

	/// <summary>
	/// Pop the consumer mode.
	/// </summary>
	protected void PopConsumerMode()
	{
		_consumerModes.TryPop(out _);
		Mode = _consumerModes.TryPeek(out var last) ? last : CodeBuilderMode.Unknown;
	}

	/// <summary>
	/// Push the consumer mode.
	/// </summary>
	/// <param name="mode"> The mode to push. </param>
	protected void PushConsumerMode(CodeBuilderMode mode)
	{
		Mode = mode;

		_consumerModes.Push(mode);
	}

	private static int CountWhile(string value, Func<char, bool> check)
	{
		var response = 0;

		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];

			if (!check(c))
			{
				return response;
			}

			response++;
		}

		return response;
	}

	private void EndObject()
	{
		switch (Mode)
		{
			case CodeBuilderMode.Array:
			{
				IndentWrite("]");
				break;
			}
			case CodeBuilderMode.Object:
			{
				IndentWrite("}");
				break;
			}
		}

		PopConsumerMode();
	}

	private void StartObject(SourceTypeInfo type)
	{
		var typeName = GetCodeTypeName(type.Type);

		switch (Settings.DesiredOutput)
		{
			case CodeBuilderOutput.Instance:
			{
				var isArray = type.Type.IsArray;
				if (isArray)
				{
					IndentWriteLine("new []");
					PushConsumerMode(CodeBuilderMode.Array);
				}
				else
				{
					IndentWriteLine($"new {typeName}");
					PushConsumerMode(CodeBuilderMode.Object);
				}
				IndentWriteLine('{');
				break;
			}
			case CodeBuilderOutput.Declaration:
			{
				var isArray = type.Type.IsArray;
				if (isArray)
				{
					IndentWriteLine("new []");
					PushConsumerMode(CodeBuilderMode.Array);
				}
				else
				{
					IndentWriteLine($"public {(type.IsStruct ? "struct" : "class")} {typeName}");
					PushConsumerMode(CodeBuilderMode.Object);
				}
				IndentWriteLine('{');
				break;
			}
		}
	}

	/// <summary>
	/// Tries to get a custom code-friendly string for a property value.
	/// Returns null if no custom handling applies.
	/// </summary>
	private string TryGetCustomCodeValue(SourceTypeInfo typeInfo, SourcePropertyInfo propertyInfo, object rawValue)
	{
		if (rawValue == null)
		{
			return null;
		}

		// 1. Check per-property providers first (highest priority)
		foreach (var provider in _propertyValueProviders)
		{
			var result = provider(typeInfo, propertyInfo.Name, rawValue);
			if (result != null)
			{
				return result;
			}
		}

		return null;
	}

	private void WriteObject<T>(SourceTypeInfo sourceInfoType, T actual)
	{
		if ((Settings.DesiredOutput == CodeBuilderOutput.Instance)
			&& TryAppendLiteral(actual))
		{
			return;
		}

		if (actual is Delegate d
			&& (d.Method.GetParameters().Length == 0))
		{
			WriteObject(d.DynamicInvoke());
			return;
		}

		StartObject(sourceInfoType);
		IncreaseIndent();

		var first = true;

		// Get properties that can be written
		var propertiesQuery = sourceInfoType
			.GetProperties()
			.Where(x => x.CanWrite);

		// Filter out default values when requested (only for Instance output)
		if (Settings.IgnoreDefaults && (Settings.DesiredOutput == CodeBuilderOutput.Instance))
		{
			propertiesQuery = propertiesQuery
				.Where(sourcePropertyInfo =>
					!ObjectExtensions.IsDefaultValue(sourceInfoType, sourcePropertyInfo, actual)
				);
		}

		var properties = propertiesQuery.ToArray();

		foreach (var property in properties)
		{
			//property.Name.Dump();

			if (!first)
			{
				AppendLine(Settings.DesiredOutput == CodeBuilderOutput.Instance ? "," : string.Empty);
			}

			WriteIndent();
			WriteProperty(sourceInfoType, property, actual);
			first = false;
		}

		DecreaseIndent();
		AppendLine();
		EndObject();
	}

	#endregion
}