#region References

using System.Linq;
using Cornerstone.Generators.Models;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Processors;

internal sealed class ChannelSubscriptionProcessor : ITypeProcessor
{
	#region Properties

	public bool EmitsInsideTypeBlock => true;

	#endregion

	#region Methods

	public void Initialize(Compilation compilation)
	{
	}

	public void Process(CSharpCodeBuilder builder, SourceTypeInfo sourceTypeInfo)
	{
		var subscriptionAttributes = sourceTypeInfo
			.Methods
			.Select(x => (x.Name, x.Attributes.FirstOrDefault(a => a.Name == Generator.NameChannelSubscriptionAttribute)))
			.Where(x => x.Item2 != null)
			.Select(x => x.Item2)
			.ToArray();

		foreach (var attr in subscriptionAttributes)
		{
			var enumType = attr.GenericTypes[0];
			var enumTypeGlobal = enumType.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName);
			var enumValue = attr.ConstructorArguments[0];
			Generator.TryGetEnumFieldName(enumType, enumValue, out var enumName);

			if (attr.GenericTypes.Length == 1)
			{

				builder.IndentWriteLine($"public void SubscribeTo{enumName}(Action handler)");
				builder.IndentWriteLine("{");
				builder.Indent++;
				builder.IndentWriteLine($"Subscribe({enumTypeGlobal}.{enumName}, handler);");
				builder.Indent--;
				builder.IndentWriteLine("}");
				builder.WriteLine();

				builder.IndentWriteLine($"public void UnsubscribeTo{enumName}(Action handler)");
				builder.IndentWriteLine("{");
				builder.Indent++;
				builder.IndentWriteLine($"Unsubscribe({enumTypeGlobal}.{enumName}, handler);");
				builder.Indent--;
				builder.IndentWriteLine("}");
				continue;
			}

			var messageValue = attr.GenericTypes[1];
			var messageTypeGlobal = messageValue.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName);

			builder.IndentWriteLine($"public void SubscribeTo{enumName}(Action<{messageTypeGlobal}> handler)");
			builder.IndentWriteLine("{");
			builder.Indent++;
			builder.IndentWriteLine($"Subscribe({enumTypeGlobal}.{enumName}, handler);");
			builder.Indent--;
			builder.IndentWriteLine("}");
			builder.WriteLine();

			builder.IndentWriteLine($"public void UnsubscribeTo{enumName}(Action<{messageTypeGlobal}> handler)");
			builder.IndentWriteLine("{");
			builder.Indent++;
			builder.IndentWriteLine($"Unsubscribe({enumTypeGlobal}.{enumName}, handler);");
			builder.Indent--;
			builder.IndentWriteLine("}");
		}
	}

	#endregion
}