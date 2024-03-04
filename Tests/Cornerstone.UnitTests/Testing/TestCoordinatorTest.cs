#region References

using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Testing;

[TestClass]
public class TestCoordinatorTest : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ShouldProcessInOrder()
	{
		var coordinator = new TestCoordinator();
		var builder = new TextBuilder();

		var task5 = Task.Run(() => coordinator.ProcessStep(5, () => builder.Append("5")));
		var task4 = Task.Run(() => coordinator.ProcessStep(4, () => builder.Append("4")));
		var task3 = Task.Run(() => coordinator.ProcessStep(3, () =>
		{
			Thread.Sleep(3);
			builder.Append("3");
		}));
		var task2 = Task.Run(() => coordinator.ProcessStep(2, () =>
		{
			Thread.Sleep(4);
			builder.Append("2");
		}));
		var task1 = Task.Run(() => coordinator.ProcessStep(1, () =>
		{
			Thread.Sleep(5);
			builder.Append("1");
		}));

		Task.WaitAll(task1, task2, task3, task4, task5);

		AreEqual("12345", builder);
	}

	#endregion
}