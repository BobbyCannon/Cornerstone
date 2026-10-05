#nullable enable

#region References

using System;
using System.Xml;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

public class XamlTestHelpers
{
	#region Methods

	public static void AssertThrowsXamlException(Action cb)
	{
		try
		{
			cb();
		}
		catch (Exception e)
		{
			if (e is XmlException)
			{
				return;
			}
			throw new Exception("Expected to throw xaml exception", e);
		}
		throw new Exception("Expected to throw xaml exception");
	}

	#endregion
}