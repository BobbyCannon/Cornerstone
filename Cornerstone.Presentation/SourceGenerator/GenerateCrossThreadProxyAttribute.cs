#region References

using System;

#endregion

namespace Cornerstone.Presentation.SourceGenerator;

[AttributeUsage(AttributeTargets.Interface)]
internal sealed class GenerateCrossThreadProxyAttribute : Attribute
{
	#region Constructors

	public GenerateCrossThreadProxyAttribute(Type priorityType, string defaultPriorityExpression)
	{
		PriorityType = priorityType;
		DefaultPriorityExpression = defaultPriorityExpression;
	}

	#endregion

	#region Properties

	public string DefaultPriorityExpression { get; }

	/// <summary>
	/// Optional. Name of the generated proxy class. Defaults to the
	/// interface name with the leading 'I' stripped (if present) and
	/// "Proxy" appended (e.g. <c> IFoo </c> → <c> FooProxy </c>).
	/// </summary>
	public string? GeneratedClassName { get; set; }

	public Type PriorityType { get; }

	#endregion
}