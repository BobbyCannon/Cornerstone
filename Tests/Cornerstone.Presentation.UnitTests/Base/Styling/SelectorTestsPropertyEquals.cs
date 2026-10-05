#region References

using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Markup.Parsers;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsPropertyEquals
{
	#region Constructors

	static SelectorTestsPropertyEquals()
	{
		//Ensure the attached properties are registered before run tests
		RuntimeHelpers.RunClassConstructor(typeof(Grid).TypeHandle);
		RuntimeHelpers.RunClassConstructor(typeof(Auth).TypeHandle);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void OfTypePropertyEqualsDoesntMatchControlOfWrongType()
	{
		var control = new TextBlock();
		var target = default(Selector).OfType<Border>().PropertyEquals(TextBlock.TextProperty, "foo");

		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, target.Match(control).Result);
	}

	[PresentationTestMethod]
	public async Task PropertyEqualsAttachedPropertyMatchingValue()
	{
		var target = new SelectorParser((ns, type) =>
		{
			return (ns, type) switch
			{
				("", nameof(TextBlock)) => typeof(TextBlock),
				("", nameof(Grid)) => typeof(Grid),
				_ => null
			};
		}).Parse("TextBlock[(Grid.Column)=1]");

		CornerstoneTest.IsNotNull(target);

		var control = new TextBlock();
		var activator = target.Match(control).Activator;
		CornerstoneTest.IsNotNull(activator);
		var observable = activator.ToObservable();

		CornerstoneTest.IsFalse(await observable.Take(1));
		Grid.SetColumn(control, 1);
		CornerstoneTest.IsTrue(await observable.Take(1));
		Grid.SetColumn(control, 0);
		CornerstoneTest.IsFalse(await observable.Take(1));
	}

	[PresentationTestMethod]
	public async Task PropertyEqualsAttachedPropertyWithNamespaceMatchingValue()
	{
		var target = new SelectorParser((ns, type) =>
		{
			return (ns, type) switch
			{
				("", nameof(TextBlock)) => typeof(TextBlock),
				("l", nameof(Auth)) => typeof(Auth),
				_ => null
			};
		}).Parse("TextBlock[(l|Auth.Name)=Admin]");

		CornerstoneTest.IsNotNull(target);

		var control = new TextBlock();
		var activator = target.Match(control).Activator;
		CornerstoneTest.IsNotNull(activator);
		var observable = activator.ToObservable();

		CornerstoneTest.IsFalse(await observable.Take(1));
		Auth.SetName(control, "Admin");
		CornerstoneTest.IsTrue(await observable.Take(1));
		Auth.SetName(control, null);
		CornerstoneTest.IsFalse(await observable.Take(1));
	}

	[PresentationTestMethod]
	public async Task PropertyEqualsMatchesWhenPropertyHasMatchingValue()
	{
		var control = new TextBlock();
		var target = default(Selector).PropertyEquals(TextBlock.TextProperty, "foo");
		var activator = target.Match(control).Activator;
		CornerstoneTest.IsNotNull(activator);
		var observable = activator.ToObservable();

		CornerstoneTest.IsFalse(await observable.Take(1));
		control.Text = "foo";
		CornerstoneTest.IsTrue(await observable.Take(1));
		control.Text = null;
		CornerstoneTest.IsFalse(await observable.Take(1));
	}

	[PresentationTestMethod]
	[DataRow("Bar", FooBar.Bar)]
	[DataRow("352", 352)]
	[DataRow("0.1", 0.1)]
	public async Task PropertyEqualsMatchesWhenPropertyHasMatchingValueAndDifferentType(string literal, object value)
	{
		var control = new TextBlock();
		var target = default(Selector).PropertyEquals(TextBlock.TagProperty, literal);
		var activator = target.Match(control).Activator;
		CornerstoneTest.IsNotNull(activator);
		var observable = activator.ToObservable();

		CornerstoneTest.IsFalse(await observable.Take(1));
		control.Tag = value;
		CornerstoneTest.IsTrue(await observable.Take(1));
		control.Tag = null;
		CornerstoneTest.IsFalse(await observable.Take(1));
	}

	[PresentationTestMethod]
	public void PropertyEqualsSelectorShouldHaveCorrectStringRepresentation()
	{
		var target = default(Selector)
			.OfType<TextBlock>()
			.PropertyEquals(TextBlock.TextProperty, "foo");

		CornerstoneTest.AreEqual("TextBlock[Text=foo]", target.ToString());
	}

	#endregion

	#region Classes

	private class Auth
	{
		#region Fields

		public static readonly AttachedProperty<string> NameProperty =
			PresentationProperty.RegisterAttached<Auth, PresentationObject, string>("Name");

		#endregion

		#region Methods

		public static string GetName(PresentationObject avaloniaObject)
		{
			return avaloniaObject.GetValue(NameProperty);
		}

		public static void SetName(PresentationObject avaloniaObject, string value)
		{
			avaloniaObject.SetValue(NameProperty, value);
		}

		#endregion
	}

	#endregion

	#region Enumerations

	private enum FooBar
	{
		Foo,
		Bar
	}

	#endregion
}