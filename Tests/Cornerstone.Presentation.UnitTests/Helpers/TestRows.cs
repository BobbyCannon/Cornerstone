#region References

using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public class TestRows : IEnumerable<object[]>
{
	#region Fields

	private readonly List<object[]> _data = [];

	#endregion

	#region Methods

	public void Add(params object[] values)
	{
		_data.Add(values);
	}

	public IEnumerator<object[]> GetEnumerator()
	{
		return _data.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}

public class TestRows<T> : IEnumerable<object[]>
{
	#region Fields

	private readonly List<object[]> _data = [];

	#endregion

	#region Methods

	public void Add(T p)
	{
		_data.Add([p]);
	}

	public IEnumerator<object[]> GetEnumerator()
	{
		return _data.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}

public class TestRows<T1, T2> : TestRows
{
	#region Methods

	public void Add(T1 p1, T2 p2)
	{
		base.Add(p1, p2);
	}

	#endregion
}

public class TestRows<T1, T2, T3> : TestRows
{
	#region Methods

	public void Add(T1 p1, T2 p2, T3 p3)
	{
		base.Add(p1, p2, p3);
	}

	#endregion
}

public class TestRows<T1, T2, T3, T4> : TestRows
{
	#region Methods

	public void Add(T1 p1, T2 p2, T3 p3, T4 p4)
	{
		base.Add(p1, p2, p3, p4);
	}

	#endregion
}