#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public abstract class BeforeAfterTestAttribute : Attribute
{
	#region Methods

	public virtual void After(MethodInfo methodUnderTest)
	{
	}

	public virtual void Before(MethodInfo methodUnderTest)
	{
	}

	#endregion
}