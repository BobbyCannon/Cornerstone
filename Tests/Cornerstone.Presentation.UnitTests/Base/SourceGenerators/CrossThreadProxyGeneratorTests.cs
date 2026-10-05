#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Presentation.SourceGenerator;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.SourceGenerators;

[TestClass]
public class CrossThreadProxyGeneratorTests
{
	#region Methods

	[PresentationTestMethod]
	public void Derivedproxyisassignabletobaseproxy()
	{
		var t = new Target();
		var m = new QueueMarshaller();
		BaseProxiedProxy proxy = new DerivedProxiedProxy(t, m.Post);

		proxy.BaseFireAndForget(3);
		m.DrainAll();
		CornerstoneTest.AreEqual(new[] { 3 }, t.BaseCalls);
	}

	[PresentationTestMethod]
	public void ExceptionintargetpropagatestoTask()
	{
		var t = new Target { GetValueImpl = () => throw new InvalidOperationException("boom") };
		var m = new QueueMarshaller();
		var proxy = new DerivedProxiedProxy(t, m.Post);

		var task = proxy.GetValue();
		m.DrainAll();
		CornerstoneTest.IsTrue(task.IsFaulted);
		CornerstoneTest.IsType<InvalidOperationException>(task.Exception!.InnerException);
	}

	[PresentationTestMethod]
	public void Explicitpriorityoverloadisused()
	{
		var t = new Target();
		var m = new QueueMarshaller();
		var proxy = new DerivedProxiedProxy(t, m.Post);

		proxy.Increment(TestPriority.High);

		CornerstoneTest.AreEqual(TestPriority.High, m.Queue.Peek().priority);
	}

	[PresentationTestMethod]
	public void Fireandforgetvoidroutesthroughmarshallerwithdefaultpriority()
	{
		var t = new Target();
		var m = new QueueMarshaller();
		var proxy = new DerivedProxiedProxy(t, m.Post);

		proxy.Increment();

		CornerstoneTest.Single(m.Queue);
		CornerstoneTest.AreEqual(TestPriority.Normal, m.Queue.Peek().priority);
		CornerstoneTest.AreEqual(0, t.Counter);
		m.DrainAll();
		CornerstoneTest.AreEqual(1, t.Counter);
	}

	[PresentationTestMethod]
	public void Inheritedmethodroutesthroughbaseproxy()
	{
		var t = new Target();
		var m = new QueueMarshaller();
		var proxy = new DerivedProxiedProxy(t, m.Post);

		proxy.BaseFireAndForget(7);
		CornerstoneTest.Single(m.Queue);
		m.DrainAll();
		CornerstoneTest.AreEqual(new[] { 7 }, t.BaseCalls);
	}

	[PresentationTestMethod]
	public void Marshallerisnotinvokedsynchronouslyduringproxycall()
	{
		var t = new Target();
		var m = new QueueMarshaller();
		var proxy = new DerivedProxiedProxy(t, m.Post);

		proxy.Increment();
		proxy.Increment();
		proxy.Increment();

		CornerstoneTest.AreEqual(0, t.Counter);
		CornerstoneTest.AreEqual(3, m.Queue.Count);
	}

	[PresentationTestMethod]
	public async Task NonVoidreturnsTaskcompletedaftermarshallerruns()
	{
		var t = new Target { Counter = 42 };
		var m = new QueueMarshaller();
		var proxy = new DerivedProxiedProxy(t, m.Post);

		var task = proxy.GetValue();
		CornerstoneTest.IsFalse(task.IsCompleted);
		m.DrainAll();
		CornerstoneTest.IsTrue(task.IsCompleted);
		CornerstoneTest.AreEqual(42, await task);
	}

	[PresentationTestMethod]
	public void Nullableproxiedparameternullispassedthrough()
	{
		var acceptsTarget = new AcceptsProxiedParamTarget();
		var m = new QueueMarshaller();
		var acceptsProxy = new AcceptsProxiedParamProxy(acceptsTarget, m.Post);

		acceptsProxy.ProcessOptionalDependency(null);
		m.DrainAll();

		CornerstoneTest.IsTrue(acceptsTarget.OptionalWasCalled);
		CornerstoneTest.IsNull(acceptsTarget.ReceivedOptionalDep);
	}

	[PresentationTestMethod]
	public void Nullableproxiedparameterproxyisunwrapped()
	{
		var depTarget = new ProxiedDependencyTarget();
		var m = new QueueMarshaller();
		var depProxy = new ProxiedDependencyProxy(depTarget, m.Post);

		var acceptsTarget = new AcceptsProxiedParamTarget();
		var acceptsProxy = new AcceptsProxiedParamProxy(acceptsTarget, m.Post);

		acceptsProxy.ProcessOptionalDependency(depProxy);
		m.DrainAll();

		CornerstoneTest.Same(depTarget, acceptsTarget.ReceivedOptionalDep);
	}

	[PresentationTestMethod]
	public void Proxiedparameterisunwrappedwhendispatching()
	{
		var depTarget = new ProxiedDependencyTarget();
		var m = new QueueMarshaller();
		var depProxy = new ProxiedDependencyProxy(depTarget, m.Post);

		var acceptsTarget = new AcceptsProxiedParamTarget();
		var acceptsProxy = new AcceptsProxiedParamProxy(acceptsTarget, m.Post);

		acceptsProxy.ProcessDependency(depProxy);
		m.DrainAll();

		CornerstoneTest.Same(depTarget, acceptsTarget.ReceivedDep);
	}

	[PresentationTestMethod]
	public void Proxydoesnotimplementproxiedinterface()
	{
		CornerstoneTest.IsFalse(typeof(IBaseProxied).IsAssignableFrom(typeof(BaseProxiedProxy)));
		CornerstoneTest.IsFalse(typeof(IDerivedProxied).IsAssignableFrom(typeof(DerivedProxiedProxy)));
	}

	[PresentationTestMethod]
	public void VoidmethodwithReturnTaskattributereturnsTask()
	{
		var t = new Target();
		var m = new QueueMarshaller();
		var proxy = new DerivedProxiedProxy(t, m.Post);

		var task = proxy.AsyncFireAndForget("hi");
		CornerstoneTest.IsFalse(task.IsCompleted);
		m.DrainAll();
		CornerstoneTest.IsTrue(task.IsCompleted);
		CornerstoneTest.AreEqual(new[] { "hi" }, t.AsyncCalls);
	}

	#endregion

	#region Interfaces

	[GenerateCrossThreadProxy(typeof(TestPriority), "Cornerstone.Presentation.UnitTests.Base.SourceGenerators.CrossThreadProxyGeneratorTests.TestPriority.Normal")]
	public interface IBaseProxied
	{
		#region Methods

		void BaseFireAndForget(int x);

		#endregion
	}

	[GenerateCrossThreadProxy(typeof(TestPriority), "Cornerstone.Presentation.UnitTests.Base.SourceGenerators.CrossThreadProxyGeneratorTests.TestPriority.Normal")]
	public interface IDerivedProxied : IBaseProxied
	{
		#region Methods

		[GenerateCrossThreadProxyReturnTask]
		void AsyncFireAndForget(string s);

		int GetValue();
		void Increment();

		#endregion
	}

	[GenerateCrossThreadProxy(typeof(TestPriority), "Cornerstone.Presentation.UnitTests.Base.SourceGenerators.CrossThreadProxyGeneratorTests.TestPriority.Normal")]
	public interface IProxiedDependency
	{
		#region Methods

		void Execute(int value);

		#endregion
	}

	[GenerateCrossThreadProxy(typeof(TestPriority), "Cornerstone.Presentation.UnitTests.Base.SourceGenerators.CrossThreadProxyGeneratorTests.TestPriority.Normal")]
	public interface IAcceptsProxiedParam
	{
		#region Methods

		void ProcessDependency(IProxiedDependency dep);
		void ProcessOptionalDependency(IProxiedDependency dep);

		#endregion
	}

	#endregion

	#region Classes

	private sealed class AcceptsProxiedParamTarget : IAcceptsProxiedParam
	{
		#region Fields

		public bool OptionalWasCalled;
		public IProxiedDependency ReceivedDep;
		public IProxiedDependency ReceivedOptionalDep;

		#endregion

		#region Methods

		public void ProcessDependency(IProxiedDependency dep)
		{
			ReceivedDep = dep;
		}

		public void ProcessOptionalDependency(IProxiedDependency dep)
		{
			ReceivedOptionalDep = dep;
			OptionalWasCalled = true;
		}

		#endregion
	}

	private sealed class ProxiedDependencyTarget : IProxiedDependency
	{
		#region Fields

		public int LastValue;

		#endregion

		#region Methods

		public void Execute(int value)
		{
			LastValue = value;
		}

		#endregion
	}

	private sealed class QueueMarshaller
	{
		#region Fields

		public readonly Queue<(Action action, TestPriority priority)> Queue = new();

		#endregion

		#region Methods

		public void DrainAll()
		{
			while (Queue.Count > 0)
			{
				Queue.Dequeue().action();
			}
		}

		public void Post(Action a, TestPriority p)
		{
			Queue.Enqueue((a, p));
		}

		#endregion
	}

	private sealed class Target : IDerivedProxied
	{
		#region Fields

		public int Counter;
		public Func<int> GetValueImpl;

		#endregion

		#region Properties

		public List<string> AsyncCalls { get; } = new();
		public List<int> BaseCalls { get; } = new();

		#endregion

		#region Methods

		public void AsyncFireAndForget(string s)
		{
			AsyncCalls.Add(s);
		}

		public void BaseFireAndForget(int x)
		{
			BaseCalls.Add(x);
		}

		public int GetValue()
		{
			return GetValueImpl?.Invoke() ?? Counter;
		}

		public void Increment()
		{
			Counter++;
		}

		#endregion
	}

	#endregion

	#region Enumerations

	public enum TestPriority
	{
		Low,
		Normal,
		High
	}

	#endregion
}