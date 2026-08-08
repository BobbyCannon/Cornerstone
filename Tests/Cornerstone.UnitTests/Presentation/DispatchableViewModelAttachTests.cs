#region References

using System;
using Cornerstone.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class DispatchableViewModelAttachTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AttachIsIdempotentPerOwner()
	{
		var vm = new HostViewModel();
		var owner = new object();

		vm.Attach(owner);
		vm.Attach(owner);
		IsTrue(vm.IsAttached);

		vm.Detach(owner);
		IsFalse(vm.IsAttached);

		// Second detach is a no-op
		vm.Detach(owner);
		IsFalse(vm.IsAttached);
	}

	[TestMethod]
	public void LateTrackDispatchChildAttachesWhenParentAlreadyAttached()
	{
		var parent = new HostViewModel();
		var view = new object();
		parent.Attach(view);

		var child = new HostViewModel();
		parent.AddChild(child);
		IsTrue(child.IsAttached);

		parent.Detach(view);
		IsFalse(parent.IsAttached);
		IsFalse(child.IsAttached);
	}

	[TestMethod]
	public void MultiOwnerStaysAttachedUntilLastDetach()
	{
		var vm = new HostViewModel();
		var a = new object();
		var b = new object();

		vm.Attach(a);
		vm.Attach(b);
		IsTrue(vm.IsAttached);

		vm.Detach(a);
		IsTrue(vm.IsAttached);

		vm.Detach(b);
		IsFalse(vm.IsAttached);
	}

	[TestMethod]
	public void NullOwnerThrows()
	{
		var vm = new HostViewModel();
		ExpectedException<ArgumentNullException>(() => vm.Attach(null));
		ExpectedException<ArgumentNullException>(() => vm.Detach(null));
		IsFalse(vm.IsAttached);
	}

	[TestMethod]
	public void ParentCascadeAttachesAndDetachesChildren()
	{
		var parent = new HostViewModel();
		var child = new HostViewModel();
		var grand = new HostViewModel();
		parent.AddChild(child);
		child.AddChild(grand);

		var view = new object();
		parent.Attach(view);

		IsTrue(parent.IsAttached);
		IsTrue(child.IsAttached);
		IsTrue(grand.IsAttached);

		parent.Detach(view);

		IsFalse(parent.IsAttached);
		IsFalse(child.IsAttached);
		IsFalse(grand.IsAttached);
	}

	[TestMethod]
	public void ParentDetachLeavesChildWithOwnViewAttached()
	{
		var parent = new HostViewModel();
		var child = new HostViewModel();
		parent.AddChild(child);

		var parentView = new object();
		var childView = new object();

		parent.Attach(parentView);
		child.Attach(childView);
		IsTrue(child.IsAttached);

		parent.Detach(parentView);
		IsFalse(parent.IsAttached);
		IsTrue(child.IsAttached); // still has childView

		child.Detach(childView);
		IsFalse(child.IsAttached);
	}

	[TestMethod]
	public void ReleaseDispatchChildDetachesParentAsOwner()
	{
		var parent = new HostViewModel();
		var child = new HostViewModel();
		parent.AddChild(child);

		parent.Attach(new object());
		IsTrue(child.IsAttached);

		parent.RemoveChild(child);
		IsFalse(child.IsAttached);
	}

	[TestMethod]
	public void ApplyModelChangesFlowsToDirectChildrenOnly()
	{
		var parent = new HostViewModel();
		var child = new HostViewModel();
		var grand = new HostViewModel();
		parent.AddChild(child);
		child.AddChild(grand);

		var pending = new DispatchPending();
		grand.RegisterPending(pending);
		pending.MarkPending();

		parent.Attach(new object());
		IsTrue(parent.HasModelChanges()); // via child → grand

		parent.ApplyModelChanges();
		IsFalse(pending.HasPending);
		IsFalse(grand.HasModelChanges());
	}

	#endregion

	#region Classes

	private sealed class HostViewModel : DispatchableViewModel
	{
		#region Methods

		public void AddChild(DispatchableViewModel child)
		{
			TrackDispatchChild(child);
		}

		public void RegisterPending(IDispatchPending pending)
		{
			TrackBinding(pending, () => { });
		}

		public void RemoveChild(DispatchableViewModel child)
		{
			ReleaseDispatchChild(child);
		}

		#endregion
	}

	#endregion
}
