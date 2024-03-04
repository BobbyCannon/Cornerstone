#region References

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

[TestClass]
public class BabelTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void UpdateDefaultValue()
	{
		AreEqual("There is a issue with the system. We will get someone to look into this.", Babel.Tower["GeneralError"]);
		Babel.Tower.AddOrUpdate("GeneralError", "FooBar");
		AreEqual("FooBar", Babel.Tower["GeneralError"]);

		Babel.Tower.AddOrUpdate("Spanish", "GeneralError", "Hello World");
		Babel.Tower.ChangeDictionary("Spanish");
		AreEqual("Hello World", Babel.Tower["GeneralError"]);

		Babel.Tower.AddOrUpdate("Spanish", "GeneralError", "Bar Foo");
		AreEqual("Bar Foo", Babel.Tower["GeneralError"]);

		Babel.Tower.ChangeDictionary("English");
		AreEqual("FooBar", Babel.Tower["GeneralError"]);

		var previous = Babel.Tower[BabelKeys.TimeServiceLocked];
		Babel.Tower.AddOrUpdate(BabelKeys.TimeServiceLocked, "Time Is Locked");
		AreNotEqual(previous, Babel.Tower[BabelKeys.TimeServiceLocked]);
		AreEqual("Time Is Locked", Babel.Tower[BabelKeys.TimeServiceLocked]);
		
		Babel.Tower.AddOrUpdate("English", BabelKeys.TimeServiceLocked, "Time Is Locked2");
		AreEqual("Time Is Locked2", Babel.Tower[BabelKeys.TimeServiceLocked]);
	}
	
	[TestMethod]
	public void CustomValue()
	{
		IsFalse(Babel.Tower.ContainsKey("Custom Error"));
		Babel.Tower.AddOrUpdate("Custom Error", "FooBar");
		IsTrue(Babel.Tower.ContainsKey("Custom Error"));
		AreEqual("FooBar", Babel.Tower["Custom Error"]);

		Babel.Tower.AddOrUpdate("English2", "Custom Error 2", "Hello World");
		Babel.Tower.ChangeDictionary("English2");
		IsTrue(Babel.Tower.ContainsKey("Custom Error 2"));
		AreEqual("Hello World", Babel.Tower["Custom Error 2"]);
		
		Babel.Tower.AddOrUpdate("English3", BabelKeys.TimeServiceLocked, "Hello World3");
		Babel.Tower.ChangeDictionary("English3");
		IsTrue(Babel.Tower.ContainsKey(BabelKeys.TimeServiceLocked));
		AreEqual("Hello World3", Babel.Tower[BabelKeys.TimeServiceLocked]);
		
		Babel.Tower.ChangeDictionary("English4");
		Babel.Tower.AddOrUpdate(BabelKeys.TimeServiceLocked, "Hello World4");
		IsTrue(Babel.Tower.ContainsKey(BabelKeys.TimeServiceLocked));
		AreEqual("Hello World4", Babel.Tower[BabelKeys.TimeServiceLocked]);
		AreEqual("Hello World4", Babel.Tower[(Enum) BabelKeys.TimeServiceLocked]);
	}
	
	[TestMethod]
	public void CustomValueForNonActiveLanguage()
	{
		AreEqual("English", Babel.Tower.Language);
		IsFalse(Babel.Tower.ContainsKey("Custom Error"));
		Babel.Tower.AddOrUpdate("Spanish", "Custom Error", "FooBar");
		
		// Should still be false because the current language is still English
		IsFalse(Babel.Tower.ContainsKey("Custom Error"));

		// Change to the Spanish language
		Babel.Tower.ChangeDictionary("Spanish");
		IsTrue(Babel.Tower.ContainsKey("Custom Error"));
		AreEqual("FooBar", Babel.Tower["Custom Error"]);
	}

	#endregion
}