#region References

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubCallLog
{
	#region Fields

	private readonly List<(string Name, object[] Args)> _calls;

	#endregion

	#region Constructors

	public StubCallLog()
	{
		_calls = new List<(string, object[])>();
	}

	#endregion

	#region Methods

	public void Add(string name, params object[] args)
	{
		_calls.Add((name, StoreArgs(args)));
	}

	public IReadOnlyList<object[]> Arguments(string name)
	{
		var result = new List<object[]>();
		for (var i = 0; i < _calls.Count; i++)
		{
			if (_calls[i].Name == name)
			{
				result.Add(UnwrapArgs(_calls[i].Args));
			}
		}

		return result;
	}

	public void Clear()
	{
		_calls.Clear();
	}

	public int Count(string name)
	{
		var count = 0;
		for (var i = 0; i < _calls.Count; i++)
		{
			if (_calls[i].Name == name)
			{
				count++;
			}
		}

		return count;
	}

	public int Count(string name, params object[] args)
	{
		var count = 0;
		for (var i = 0; i < _calls.Count; i++)
		{
			if ((_calls[i].Name == name) && ArgsEqual(UnwrapArgs(_calls[i].Args), args))
			{
				count++;
			}
		}

		return count;
	}

	public IReadOnlyList<string> Names()
	{
		var result = new List<string>(_calls.Count);
		for (var i = 0; i < _calls.Count; i++)
		{
			result.Add(_calls[i].Name);
		}

		return result;
	}

	public void VerifyCalled(string name)
	{
		if (!WasCalled(name))
		{
			throw new AssertFailedException("Expected " + name + " to be called.");
		}
	}

	public void VerifyCalled(string name, int times)
	{
		var count = Count(name);
		if (count != times)
		{
			throw new AssertFailedException("Expected " + name + " to be called " + times + " time(s), actually " + count + ".");
		}
	}

	public void VerifyCalledAtLeastOnce(string name)
	{
		if (Count(name) < 1)
		{
			throw new AssertFailedException("Expected " + name + " to be called.");
		}
	}

	public void VerifyLastPrefix(string name, params object[] prefix)
	{
		var args = Arguments(name);
		if (args.Count == 0)
		{
			throw new AssertFailedException("Expected " + name + " to be called.");
		}

		var last = args[args.Count - 1];
		if ((prefix != null) && (last.Length < prefix.Length))
		{
			throw new AssertFailedException("Expected " + name + " arguments to start with " + prefix.Length + " value(s).");
		}

		for (var i = 0; i < prefix.Length; i++)
		{
			if (!Equals(prefix[i], last[i]))
			{
				throw new AssertFailedException("Expected " + name + " argument " + i + " to be " + prefix[i] + ", actually " + last[i] + ".");
			}
		}
	}

	public void VerifyNotCalled(string name)
	{
		if (WasCalled(name))
		{
			throw new AssertFailedException("Expected " + name + " not to be called.");
		}
	}

	public bool WasCalled(string name)
	{
		return Count(name) > 0;
	}

	public bool WasCalled(string name, params object[] args)
	{
		return Count(name, args) > 0;
	}

	private static bool ArgsEqual(object[] left, object[] right)
	{
		if (left.Length != right.Length)
		{
			return false;
		}

		for (var i = 0; i < left.Length; i++)
		{
			if (!Equals(left[i], right[i]))
			{
				return false;
			}
		}

		return true;
	}

	private static object[] StoreArgs(object[] args)
	{
		if ((args == null) || (args.Length == 0))
		{
			return Array.Empty<object>();
		}

		var stored = new object[args.Length];
		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i];
			if ((arg != null) && !arg.GetType().IsValueType)
			{
				stored[i] = new WeakReference(arg);
			}
			else
			{
				stored[i] = arg;
			}
		}

		return stored;
	}

	private static object[] UnwrapArgs(object[] args)
	{
		if ((args == null) || (args.Length == 0))
		{
			return Array.Empty<object>();
		}

		var unwrapped = new object[args.Length];
		for (var i = 0; i < args.Length; i++)
		{
			unwrapped[i] = args[i] is WeakReference weak ? weak.Target : args[i];
		}

		return unwrapped;
	}

	#endregion
}