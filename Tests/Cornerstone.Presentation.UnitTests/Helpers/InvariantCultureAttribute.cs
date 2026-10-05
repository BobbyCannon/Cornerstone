#region References

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

/// <summary>
/// Runs tests in the invariant culture.
/// </summary>
/// <remarks>
/// Some tests check exception messages, and those from the .NET framework will be translated.
/// Some tests are formatting numbers, expecting a dot as a decimal point.
/// Use this fixture to set the current culture to the invariant culture.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
[TestClass]
public sealed class InvariantCultureAttribute : BeforeAfterTestAttribute
{
	#region Fields

	private CultureInfo _previousCulture;
	private CultureInfo _previousUICulture;

	#endregion

	#region Methods

	public override void After(MethodInfo methodUnderTest)
	{
		CultureInfo.CurrentCulture = _previousCulture!;
		CultureInfo.CurrentUICulture = _previousUICulture!;

		base.After(methodUnderTest);
	}

	public override void Before(MethodInfo methodUnderTest)
	{
		base.Before(methodUnderTest);

		_previousCulture = CultureInfo.CurrentCulture;
		_previousUICulture = CultureInfo.CurrentUICulture;

		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
	}

	#endregion
}