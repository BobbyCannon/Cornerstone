#region References

using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyShared;

public class EventInvokerMethod
{
	#region Fields

	public InvokerTypes InvokerType;
	public bool IsVisibleFromChildren;
	public MethodReference MethodReference;

	#endregion
}