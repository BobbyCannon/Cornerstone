#region References

using System.Linq;
using Cornerstone.Generators.Models;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators;

public partial class Generator
{
	#region Methods

	private static void ProcessRelayCommands(CSharpCodeBuilder builder, SourceTypeInfo sourceTypeInfo)
	{
		var relayMethods = sourceTypeInfo
			.Methods
			.Select(x => (x.Name, x.Attributes.FirstOrDefault(a => a.Name == NameRelayCommandAttribute)))
			.Where(x => x.Item2 != null)
			.ToDictionary(x => x.Name, x => x.Item2);

		foreach (var method in relayMethods)
		{
			var member = (IMethodSymbol) sourceTypeInfo.TypeSymbol.GetMembers(method.Key).First();
			var f = CalculateFieldName(member) + "Command";
			var p = CalculatePropertyName(member) + "Command";

			var canExecuteMethodArgument = method.Value.NamedArguments.FirstOrDefault(a => a.Key == "CanExecuteMethod").Value as string;
			var canExecuteMethod = sourceTypeInfo.Methods.FirstOrDefault(x => x.Name == canExecuteMethodArgument);

			builder.IndentWriteLine($"private {GlobalRelayCommand} {f};");
			builder.IndentWriteLine($"public {GlobalSystemICommand} {p}");
			builder.IndentWriteLine("{");
			builder.Indent++;

			var parameterType = member.Parameters.FirstOrDefault()?.Type;
			var cast = (parameterType != null) && (parameterType.SpecialType != SpecialType.System_Object)
				? $"({parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}) "
				: "";
			var prefix = member.IsAsync ? "_ = " : "";
			var firstParameter = member.Parameters.Count() == 1
				? $"x => {prefix}{member.Name}({cast}x)"
				: $"_ => {prefix}{member.Name}()";
			var secondParameter = canExecuteMethod == null
				? string.Empty
				: canExecuteMethod.Parameters.Count() == 1
					? $", x => {canExecuteMethod.Name}(x)"
					: $", _ => {canExecuteMethod.Name}()";

			builder.IndentWriteLine($"get {{ return {f} ??= new {GlobalRelayCommand}({firstParameter}{secondParameter}); }}");
			builder.Indent--;
			builder.IndentWriteLine("}");
		}
	}

	#endregion
}