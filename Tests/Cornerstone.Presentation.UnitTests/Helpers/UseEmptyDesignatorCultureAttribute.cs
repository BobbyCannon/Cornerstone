#region References

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[TestClass]
public sealed class UseEmptyDesignatorCultureAttribute : BeforeAfterTestAttribute
{
	#region Fields

	private CultureInfo _previousCulture;
	private CultureInfo _previousUICulture;

	#endregion

	#region Properties

	private CultureInfo CultureInfo { get; } =
		new(string.Empty, false) { DateTimeFormat = { AMDesignator = string.Empty, PMDesignator = string.Empty } };

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

		CultureInfo.CurrentCulture = CultureInfo;
		CultureInfo.CurrentUICulture = CultureInfo;
	}

	#endregion
}