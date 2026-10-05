#region References

using System;
using Cornerstone.Data;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public partial class ModelManagerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddOrUpdateInsertsThenUpdatesByDistinctCheck()
	{
		var manager = new TestModelManager(this, this);
		manager.InitializeLifecycle();

		var first = manager.AddOrUpdate(new TestModel { SyncId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "A" });
		AreEqual(1, manager.Count);
		AreEqual("A", first.Name);

		var second = manager.AddOrUpdate(new TestModel { SyncId = first.SyncId, Name = "B" });
		AreEqual(1, manager.Count);
		AreEqual(first, second);
		AreEqual("B", second.Name);
	}

	[TestMethod]
	public void ResetClearsSelectionAndLastUpdated()
	{
		var manager = new TestModelManager(this, this);
		manager.InitializeLifecycle();
		manager.AddOrUpdate(new TestModel { SyncId = Guid.NewGuid(), Name = "A" });
		manager.SelectedItem = manager[0];

		manager.Reset();

		AreEqual(0, manager.Count);
		IsNull(manager.SelectedItem);
		AreEqual(DateTime.MinValue, manager.LastUpdated);
	}

	#endregion

	#region Classes

	[Updateable(UpdateableAction.All, ["*"])]
	private sealed partial class TestModel : CornerstoneObject
	{
		#region Properties

		public string Name { get; set; }

		public Guid SyncId { get; set; }

		#endregion
	}

	private sealed class TestModelManager : ModelManager<TestModel>
	{
		#region Constructors

		public TestModelManager(IDateTimeProvider dateTimeProvider, IDependencyProvider dependencyProvider)
			: base(dateTimeProvider, dependencyProvider, (x, y) => x.SyncId == y.SyncId)
		{
		}

		#endregion

		#region Methods

		protected override TestModel CreateModel()
		{
			return new TestModel();
		}

		#endregion
	}

	#endregion
}