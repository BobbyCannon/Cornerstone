#region References

using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class BindableTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ApplyChangesTo()
	{
		var actual = new TestClass { Age = 21, Name = "Foo Bar" };
		actual.ResetHasChanges();

		var update = new TestClass { Age = 22 };
		update.ApplyChangesTo(actual);

		AreEqual(22, actual.Age);
		AreEqual("Foo Bar", actual.Name);
	}

	[TestMethod]
	public void HasChanges()
	{
		var test = new TestClass
		{
			Age = 21,
			Name = "Foo Bar"
		};
		var json = test.ToJson();
		var actual = json.FromJson<TestClass>();
		IsFalse(actual.HasChanges());

		var shallow = test.ShallowClone();
		IsFalse(shallow.HasChanges());

		var deep = test.DeepClone();
		IsFalse(deep.HasChanges());

		actual.Age = 22;
		IsTrue(actual.HasChanges());
		AreEqual(new ReadOnlySet<string>(["Age"]), actual.GetChangedProperties());

		actual.ResetHasChanges();
		AreEqual(0, actual.GetChangedProperties().Count);

		actual.Name = "Hello World";
		IsTrue(actual.HasChanges());
		AreEqual(new ReadOnlySet<string>(["Name"]), actual.GetChangedProperties());
	}

	[TestMethod]
	public void OnPropertyChangingAndChanged()
	{
		var changed = new List<string>();
		var changing = new List<string>();
		var test = new TestClass();
		test.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
		test.PropertyChanging += (_, args) => changing.Add(args.PropertyName);
		test.Name = "Test";
		AreEqual(new[] { "Name" }, changed);
		AreEqual(new[] { "Name" }, changing);
	}

	#endregion

	#region Classes

	public class TestClass : Bindable<TestClass>
	{
		#region Properties

		public int Age { get; set; }

		public string Name { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Update the TestClass with an update.
		/// </summary>
		/// <param name="update"> The update to be applied. </param>
		/// <param name="settings"> The options for controlling the updating of the entity. </param>
		public override bool UpdateWith(TestClass update, IncludeExcludeSettings settings)
		{
			// If the update is null then there is nothing to do.
			if (update == null)
			{
				return false;
			}

			// ****** You can use GenerateUpdateWith to update this ******

			if ((settings == null) || settings.IsEmpty())
			{
				Age = update.Age;
				Name = update.Name;
			}
			else
			{
				this.IfThen(_ => settings.ShouldProcessProperty(nameof(Age)), x => x.Age = update.Age);
				this.IfThen(_ => settings.ShouldProcessProperty(nameof(Name)), x => x.Name = update.Name);
			}

			return true;
		}

		#endregion
	}

	#endregion
}