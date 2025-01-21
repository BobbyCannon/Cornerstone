#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators.CSharpGenerators;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators.CodeGenerators;

/// <inheritdoc />
public class CSharpCodeWriter : CodeWriter<ICodeWriterSettings>
{
	#region Constants

	public const string SlashSectionFormat = "// Code Generated - {0}";
	public const string XmlSectionFormat = "<!-- Code Generated - {0} -->";

	#endregion

	#region Fields

	private static readonly IList<ICodeGenerator> _builtInGenerators;
	private static readonly ReadOnlyDictionary<Type, string> _simplifiedTypeNames;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public CSharpCodeWriter()
		: this(new CodeWriterSettings { TextFormat = TextFormat.Indented })
	{
	}

	/// <inheritdoc />
	public CSharpCodeWriter(ICodeWriterSettings settings)
		: base(settings ?? Generators.CodeGenerator.DefaultWriterSettings)
	{
	}

	/// <summary>
	/// A code consumer for CSharp (C#).
	/// </summary>
	static CSharpCodeWriter()
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

		_builtInGenerators = new List<ICodeGenerator>
		{
			new SystemDrawingGenerator(),
			new NumberCSharpGenerator(),
			new StringCSharpGenerator(),
			new TimeCSharpGenerator(),
			new EnumCSharpGenerator(),
			new DateCSharpGenerator(),
			new GuidCSharpGenerator(),
			new DictionaryCSharpGenerator(),
			new ListCSharpGenerator(),
			new JsonValueCSharpGenerator(),
			new VersionCSharpGenerator()
		};
	}

	#endregion

	#region Methods

	/// <summary>
	/// Convert the value to CSharp code.
	/// </summary>
	/// <param name="value"> The value to write. </param>
	/// <param name="settings"> Optional settings. </param>
	/// <returns> The CSharp code. </returns>
	public static string GenerateCode(object value, ICodeWriterSettings settings = null)
	{
		var writer = new CSharpCodeWriter(settings);
		writer.AppendObject(value);
		return writer.ToString();
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
		var index = typeName.IndexOf("`");
		if (index >= 0)
		{
			typeName = typeName.Substring(0, index);
		}

		var genericArguments = type.GetGenericArguments();
		return $"{typeName}<{string.Join(", ", genericArguments.Select(GetCodeTypeName))}>";
	}

	/// <summary>
	/// Converts MethodInfo to the code type. Ex. Build&lt;T1, T2&gt; or Build&lt;Account, int&gt;
	/// </summary>
	/// <param name="info"> The MethodInfo to convert. </param>
	/// <returns> The method info in a CSharp code format. </returns>
	public static string GetCodeTypeName(MethodInfo info)
	{
		if (info.IsGenericMethodDefinition)
		{
			var genericArguments = info.GetGenericArguments();
			return $"{info.Name}<{string.Join(", ", genericArguments.Select(GetCodeTypeName))}>";
		}

		if (info.IsGenericMethod)
		{
			var genericArguments = info.GetGenericArguments();
			return $"{info.Name}<{string.Join(", ", genericArguments.Select(GetCodeTypeName))}>";
		}

		return info.Name;
	}

	/// <inheritdoc />
	public override ICodeGenerator GetGenerator(object value)
	{
		return _builtInGenerators.FirstOrDefault(x => x.SupportsType(value?.GetType()));
	}

	/// <inheritdoc />
	public override IObjectConsumer StartObject(Type type)
	{
		switch (Settings.OutputMode)
		{
			case CodeWriterMode.Declaration:
			{
				var isArray = type.IsArray;
				if (isArray)
				{
					Append("new []");
					PushConsumerMode(ObjectConsumerMode.Array);
				}
				else
				{
					StartObjectDeclaration(type.IsClass, GetCodeTypeName(type));
					return this;
				}
				break;
			}
			case CodeWriterMode.Instance:
			default:
			{
				var isArray = type.IsArray;
				if (isArray)
				{
					Append("new []");
					PushConsumerMode(ObjectConsumerMode.Array);
				}
				else
				{
					Append($"new {GetCodeTypeName(type)}");
					PushConsumerMode(ObjectConsumerMode.Object);
				}
				break;
			}
		}

		NewLine();
		AppendLineThenPushIndent("{");

		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer WriteProperty(string name, object value)
	{
		switch (Settings.OutputMode)
		{
			case CodeWriterMode.Declaration:
			{
				WriteRawString("public ");
				WriteRawString(GetCodeTypeName(value.GetType()));
				WriteRawString(" ");
				WriteRawString(name);
				WriteRawString(" { get; set; }");
				NewLine();
				break;
			}
			default:
			{
				WriteRawString(name);
				WriteRawString(" = ");
				WritePropertyValue(value);
				WriteRawString(",");
				NewLine();
				break;
			}
		}
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer WriteProperty(PropertyInfo info, object value)
	{
		switch (Settings.OutputMode)
		{
			case CodeWriterMode.Declaration:
			{
				WriteRawString("public ");
				WriteRawString(GetCodeTypeName(info.PropertyType));
				WriteRawString(" ");
				WriteRawString(info.Name);
				WriteRawString(";");
				break;
			}
			default:
			{
				WriteRawString(info.Name);
				WriteRawString(" = ");
				WritePropertyValue(value);
				break;
			}
		}
		return this;
	}

	private void StartObjectDeclaration(bool isClass, string name)
	{
		Append($"public {(isClass ? "class" : "struct")} {name}()");
		PushConsumerMode(ObjectConsumerMode.Object);
		NewLine();
		AppendLineThenPushIndent("{");
	}

	#endregion
}