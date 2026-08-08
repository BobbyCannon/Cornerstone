#region References

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class DispatchableViewModelBindingTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void TrackBindingAppliesAndClearsPending()
	{
		var pending = new DispatchPending();
		var applied = 0;
		var vm = new BindingHostViewModel();
		vm.RegisterBinding(pending, () => applied++);

		IsFalse(vm.HasModelChanges());

		pending.MarkPending();
		IsTrue(vm.HasModelChanges());

		vm.ApplyModelChanges();
		AreEqual(1, applied);
		IsFalse(pending.HasPending);
		IsFalse(vm.HasModelChanges());
	}

	[TestMethod]
	public void TrackCollectionReconcilesWhenSourcePending()
	{
		var source = new SpeedyList<string>(8);
		var destination = new List<string>();
		var vm = new BindingHostViewModel();
		vm.RegisterCollection(source, destination);

		source.Add("a");
		source.Add("b");
		IsTrue(vm.HasModelChanges());

		vm.ApplyModelChanges();
		AreEqual(2, destination.Count);
		AreEqual("a", destination[0]);
		AreEqual("b", destination[1]);
		IsFalse(source.HasPending);
		IsFalse(vm.HasModelChanges());

		source.Add("c");
		vm.ApplyModelChanges();
		AreEqual(3, destination.Count);
		AreEqual("c", destination[2]);
	}

	[TestMethod]
	public void TrackCollectionSurvivesConcurrentShrink()
	{
		// Reproduces the sample race: Count is read, then the list shrinks before
		// CopyTo finishes, which used to leave null slots and crash Dictionary.TryGetValue.
		var source = new SpeedyList<string>(64, isLongLivedBuffer: true);
		var destination = new PresentationList<string>();
		var vm = new BindingHostViewModel();
		vm.RegisterCollection(source, destination, CollectionReconcileMode.ListAndItems);

		for (var i = 0; i < 20; i++)
		{
			source.Add($"item-{i}");
		}

		vm.ApplyModelChanges();
		AreEqual(20, destination.Count);

		using var cts = new System.Threading.CancellationTokenSource();
		var producer = Task.Run(async () =>
		{
			var id = 20;
			while (!cts.Token.IsCancellationRequested)
			{
				if (source.Count < 30)
				{
					source.Add($"item-{id++}");
				}

				if (source.Count > 0)
				{
					source.RemoveAt(Random.Shared.Next(source.Count));
				}

				await Task.Delay(1);
			}
		});

		try
		{
			for (var i = 0; i < 100; i++)
			{
				source.Add($"force-{i}"); // ensure pending
				vm.ApplyModelChanges();
			}
		}
		finally
		{
			cts.Cancel();
			producer.Wait(TimeSpan.FromSeconds(2));
		}

		// Final apply should not throw and destination should only contain non-null strings.
		vm.ApplyModelChanges();
		foreach (var item in destination)
		{
			IsNotNull(item);
		}
	}

	[TestMethod]
	public void TrackIngressDrainsToConsumer()
	{
		var ingress = new TextIngress();
		var builder = new StringBuilder();
		var vm = new BindingHostViewModel();
		vm.RegisterIngress(ingress, span => builder.Append(span));

		IsFalse(vm.HasModelChanges());

		ingress.Append("hi");
		IsTrue(vm.HasModelChanges());

		vm.ApplyModelChanges();
		AreEqual("hi", builder.ToString());
		IsFalse(ingress.HasPending);
		IsFalse(vm.HasModelChanges());

		ingress.Append("!");
		vm.ApplyModelChanges();
		AreEqual("hi!", builder.ToString());
	}

	[TestMethod]
	public void TrackIngressMultipleSourcesSameDestination()
	{
		var a = new TextIngress();
		var b = new TextIngress();
		var builder = new StringBuilder();
		var vm = new BindingHostViewModel();
		vm.RegisterIngress(a, span => builder.Append(span));
		vm.RegisterIngress(b, span => builder.Append(span));

		a.Append("A");
		b.Append("B");
		vm.ApplyModelChanges();
		AreEqual("AB", builder.ToString());
	}

	#endregion

	#region Classes

	private sealed class BindingHostViewModel : DispatchableViewModel
	{
		#region Methods

		public void RegisterBinding(IDispatchPending pending, System.Action apply)
		{
			TrackBinding(pending, apply);
		}

		public void RegisterCollection(
			IList<string> source,
			IList<string> destination,
			CollectionReconcileMode mode = CollectionReconcileMode.List)
		{
			TrackCollection(source, destination, mode: mode);
		}

		public void RegisterIngress(TextIngress source, System.Action<System.ReadOnlySpan<char>> consumer)
		{
			TrackIngress(source, consumer);
		}

		#endregion
	}

	#endregion
}
