#region References

using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Fody;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public bool FoundInterceptor;
	public MethodDefinition InterceptMethod;
	public InvokerTypes InterceptorType;

	#endregion

	#region Methods

	public void FindInterceptor()
	{
		var typeDefinition = ModuleDefinition.Types.FirstOrDefault(_ => _.Name == "PropertyChangedNotificationInterceptor");
		if (typeDefinition == null)
		{
			FoundInterceptor = false;
			return;
		}
		SearchForMethod(typeDefinition);
	}

	public bool IsBeforeAfterInterceptionMethod(MethodDefinition method)
	{
		var parameters = method.Parameters;
		return (parameters.Count == 5)
			&& (parameters[0].ParameterType.FullName == "System.Object")
			&& (parameters[1].ParameterType.FullName == "System.Action")
			&& (parameters[2].ParameterType.FullName == "System.String")
			&& (parameters[3].ParameterType.FullName == "System.Object")
			&& (parameters[4].ParameterType.FullName == "System.Object");
	}

	public bool IsSingleStringInterceptionMethod(MethodDefinition method)
	{
		var parameters = method.Parameters;
		return (parameters.Count == 3)
			&& (parameters[0].ParameterType.FullName == "System.Object")
			&& (parameters[1].ParameterType.FullName == "System.Action")
			&& (parameters[2].ParameterType.FullName == "System.String");
	}

	private void SearchForMethod(TypeDefinition typeDefinition)
	{
		var methodDefinition = typeDefinition.Methods.FirstOrDefault(_ => _.Name == "Intercept");
		if (methodDefinition == null)
		{
			throw new WeavingException($"Found Type '{typeDefinition.FullName}' but could not find a method named 'Intercept'.");
		}
		if (!methodDefinition.IsStatic)
		{
			throw new WeavingException($"Found Type '{typeDefinition.FullName}.Intercept' but it is not static.");
		}
		if (!methodDefinition.IsPublic)
		{
			throw new WeavingException($"Found Type '{typeDefinition.FullName}.Intercept' but it is not public.");
		}

		if (IsSingleStringInterceptionMethod(methodDefinition))
		{
			FoundInterceptor = true;
			InterceptMethod = methodDefinition;
			InterceptorType = InvokerTypes.String;
			return;
		}
		if (IsBeforeAfterInterceptionMethod(methodDefinition))
		{
			FoundInterceptor = true;
			InterceptMethod = methodDefinition;
			InterceptorType = InvokerTypes.BeforeAfter;
			return;
		}
		var message = $"""
						Found '{typeDefinition.FullName}.Intercept' But the signature is not correct. It needs to be either.
						Intercept(object target, Action firePropertyChanged, string propertyName)
						or
						Intercept(object target, Action firePropertyChanged, string propertyName, object before, object after)
						""";
		throw new WeavingException(message);
	}

	#endregion
}