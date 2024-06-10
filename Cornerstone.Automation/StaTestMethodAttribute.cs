#region References

using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Automation;

/// <summary>
/// A test method attribute to force the test on an STA thread.
/// </summary>
public class StaTestMethodAttribute : TestMethodAttribute
{
	#region Methods

	/// <inheritdoc />
	public override TestResult[] Execute(ITestMethod testMethod)
	{
		if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
		{
			return Invoke(testMethod);
		}

		TestResult[] result = null;
		var thread = new Thread(() => result = Invoke(testMethod));
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		return result;
	}

	private TestResult[] Invoke(ITestMethod testMethod)
	{
		return [testMethod.Invoke(null)];
	}

	#endregion
}