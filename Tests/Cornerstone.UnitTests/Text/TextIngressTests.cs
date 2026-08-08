#region References

using System;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class TextIngressTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AppendEmptyDoesNotSetPending()
	{
		var ingress = new TextIngress();
		ingress.Append(string.Empty);
		ingress.Append(ReadOnlySpan<char>.Empty);

		IsFalse(ingress.HasPending);
		AreEqual(0, ingress.PendingCount);
		AreEqual(0, ingress.DrainTo(new StringBuffer()));
	}

	[TestMethod]
	public void ClearDropsPendingWithoutDestination()
	{
		var ingress = new TextIngress();
		ingress.Append("hello");
		ingress.Clear();

		IsFalse(ingress.HasPending);
		AreEqual(0, ingress.DrainTo(new StringBuffer()));
	}

	[TestMethod]
	public void DrainEmptyReturnsZero()
	{
		var ingress = new TextIngress();
		var destination = new StringBuffer();

		AreEqual(0, ingress.DrainTo(destination));
		AreEqual(0, destination.Count);
	}

	[TestMethod]
	public void DrainMovesBatchToDestination()
	{
		var ingress = new TextIngress();
		var destination = new StringBuffer();

		ingress.Append("Hello");
		ingress.Append(' ');
		ingress.Append("World");

		IsTrue(ingress.HasPending);
		AreEqual(11, ingress.PendingCount);

		var drained = ingress.DrainTo(destination);

		AreEqual(11, drained);
		IsFalse(ingress.HasPending);
		AreEqual(0, ingress.PendingCount);
		AreEqual("Hello World", destination.ToString());
	}

	[TestMethod]
	public void DrainViaConsumerReceivesSpan()
	{
		var ingress = new TextIngress();
		ingress.Append("abc");

		string captured = null;
		var drained = ingress.Drain(span => captured = span.ToString());

		AreEqual(3, drained);
		AreEqual("abc", captured);
		IsFalse(ingress.HasPending);
	}

	[TestMethod]
	public void MultipleDrainsCoalesceBetweenTicks()
	{
		var ingress = new TextIngress();
		var destination = new StringBuffer();

		ingress.Append("one");
		AreEqual(3, ingress.DrainTo(destination));

		ingress.Append(" two");
		AreEqual(4, ingress.DrainTo(destination));

		AreEqual("one two", destination.ToString());
	}

	[TestMethod]
	public void ParallelAppendsAreCoalescedOnDrain()
	{
		var ingress = new TextIngress(1024);
		const int writers = 8;
		const int charsPerWriter = 200;

		Parallel.For(0, writers, i =>
		{
			var chunk = new string((char) ('a' + i), charsPerWriter);
			for (var n = 0; n < charsPerWriter; n++)
			{
				ingress.Append(chunk[n]);
			}
		});

		var destination = new StringBuffer();
		var drained = ingress.DrainTo(destination);

		AreEqual(writers * charsPerWriter, drained);
		AreEqual(writers * charsPerWriter, destination.Count);
		IsFalse(ingress.HasPending);

		// Every writer contributed charsPerWriter of the same letter
		for (var i = 0; i < writers; i++)
		{
			var letter = (char) ('a' + i);
			AreEqual(charsPerWriter, destination.AsSpan().ToArray().Count(c => c == letter));
		}
	}

	[TestMethod]
	public void ProducerCanAppendWhileConsumerHoldsBatch()
	{
		var ingress = new TextIngress();
		ingress.Append("first");

		var firstBatch = string.Empty;
		ingress.Drain(span =>
		{
			firstBatch = span.ToString();
			// Simulate producer writing during drain consumer
			ingress.Append("second");
		});

		AreEqual("first", firstBatch);
		IsTrue(ingress.HasPending);

		var destination = new StringBuffer();
		AreEqual(6, ingress.DrainTo(destination));
		AreEqual("second", destination.ToString());
	}

	#endregion
}
