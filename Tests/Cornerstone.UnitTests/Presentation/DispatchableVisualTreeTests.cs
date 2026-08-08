#region References

using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class DispatchableVisualTreeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AttachedAndDetachedWithViewModelOnly()
	{
		var owner = new object();
		var vm = new HostViewModel();

		DispatchableVisualTree.OnAttachedToVisualTree(owner, vm, null);
		IsTrue(vm.IsAttached);

		DispatchableVisualTree.OnDetachedFromVisualTree(owner, vm, null);
		IsFalse(vm.IsAttached);
	}

	[TestMethod]
	public void AttachedAndDetachedWithDataContextOnly()
	{
		var owner = new object();
		var dc = new HostViewModel();

		DispatchableVisualTree.OnAttachedToVisualTree(owner, null, dc);
		IsTrue(dc.IsAttached);

		DispatchableVisualTree.OnDetachedFromVisualTree(owner, null, dc);
		IsFalse(dc.IsAttached);
	}

	[TestMethod]
	public void SameInstanceOnViewModelAndDataContextIsSingleOwner()
	{
		var owner = new object();
		var shared = new HostViewModel();

		DispatchableVisualTree.OnAttachedToVisualTree(owner, shared, shared);
		IsTrue(shared.IsAttached);

		// One detach path with both args is enough (idempotent).
		DispatchableVisualTree.OnDetachedFromVisualTree(owner, shared, shared);
		IsFalse(shared.IsAttached);
	}

	[TestMethod]
	public void DifferentViewModelAndDataContextBothAttach()
	{
		var owner = new object();
		var vm = new HostViewModel();
		var dc = new HostViewModel();

		DispatchableVisualTree.OnAttachedToVisualTree(owner, vm, dc);
		IsTrue(vm.IsAttached);
		IsTrue(dc.IsAttached);

		DispatchableVisualTree.OnDetachedFromVisualTree(owner, vm, dc);
		IsFalse(vm.IsAttached);
		IsFalse(dc.IsAttached);
	}

	[TestMethod]
	public void ViewModelChangeWhileOnTreeKeepsDataContextOwner()
	{
		var owner = new object();
		var vm1 = new HostViewModel();
		var vm2 = new HostViewModel();
		var dc = new HostViewModel();

		DispatchableVisualTree.OnAttachedToVisualTree(owner, vm1, dc);
		IsTrue(vm1.IsAttached);
		IsTrue(dc.IsAttached);

		DispatchableVisualTree.OnViewModelChanged(owner, vm1, vm2, dc, onVisualTree: true);
		IsFalse(vm1.IsAttached);
		IsTrue(vm2.IsAttached);
		IsTrue(dc.IsAttached);
	}

	[TestMethod]
	public void ViewModelChangeDoesNotDetachIfDataContextStillHoldsInstance()
	{
		var owner = new object();
		var shared = new HostViewModel();
		var vm2 = new HostViewModel();

		DispatchableVisualTree.OnAttachedToVisualTree(owner, shared, shared);
		IsTrue(shared.IsAttached);

		// ViewModel moves away; DataContext still shared — must stay attached.
		DispatchableVisualTree.OnViewModelChanged(owner, shared, vm2, shared, onVisualTree: true);
		IsTrue(shared.IsAttached);
		IsTrue(vm2.IsAttached);
	}

	#endregion

	#region Classes

	private sealed class HostViewModel : DispatchableViewModel
	{
	}

	#endregion
}
