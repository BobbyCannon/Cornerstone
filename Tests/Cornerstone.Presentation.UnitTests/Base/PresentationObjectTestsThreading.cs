#region References

using System;
using System.Reactive.Subjects;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsThreading : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DirectPropertyClearValueShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			AssertThrowsOnDifferentThread(() => target.ClearValue(Class1.DirectProperty));
		}
	}

	[PresentationTestMethod]
	public void DirectPropertyGetValueShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			AssertThrowsOnDifferentThread(() => target.GetValue(Class1.DirectProperty));
		}
	}

	[PresentationTestMethod]
	public void DirectPropertyIsSetShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			AssertThrowsOnDifferentThread(() => target.IsSet(Class1.DirectProperty));
		}
	}

	[PresentationTestMethod]
	public void DirectPropertySetValueShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			AssertThrowsOnDifferentThread(() => target.SetValue(Class1.DirectProperty, "foo"));
		}
	}

	[PresentationTestMethod]
	public void SettingDirectPropertyBindingShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			AssertThrowsOnDifferentThread(() =>
				target.Bind(
					Class1.DirectProperty,
					new BehaviorSubject<string>("foo")));
		}
	}

	[PresentationTestMethod]
	public void SettingStyledPropertyBindingShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			AssertThrowsOnDifferentThread(() =>
				target.Bind(
					Class1.StyledProperty,
					new BehaviorSubject<string>("foo")));
		}
	}

	[PresentationTestMethod]
	public void StyledPropertyClearValueShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			AssertThrowsOnDifferentThread(() => target.ClearValue(Class1.StyledProperty));
		}
	}

	[PresentationTestMethod]
	public void StyledPropertyGetValueShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			target.GetValue(Class1.StyledProperty);

			AssertThrowsOnDifferentThread(() => target.GetValue(Class1.StyledProperty));
		}
	}

	[PresentationTestMethod]
	public void StyledPropertyIsSetShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			AssertThrowsOnDifferentThread(() => target.IsSet(Class1.StyledProperty));
		}
	}

	[PresentationTestMethod]
	public void StyledPropertySetValueShouldThrow()
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			AssertThrowsOnDifferentThread(() => target.SetValue(Class1.StyledProperty, "foo"));
		}
	}

	private void AssertThrowsOnDifferentThread(Action cb)
	{
		Assert.Throws<InvalidOperationException>(() =>
			ThreadRunHelper.RunOnDedicatedThread(cb).GetAwaiter().GetResult());
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly DirectProperty<Class1, string> DirectProperty =
			PresentationProperty.RegisterDirect<Class1, string>("Qux", _ => null, (o, v) => { });

		public static readonly StyledProperty<string> StyledProperty =
			PresentationProperty.Register<Class1, string>("Foo", "foodefault");

		#endregion
	}

	#endregion
}