#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

public class ParamEventArgs<T> : EventArgs
{
	#region Constructors

	public ParamEventArgs(T param)
	{
		Param = param;
	}

	#endregion

	#region Properties

	public T Param { get; set; }

	#endregion
}

[TestClass]
public class TestVisual : Visual
{
	#region Properties

	public Visual Child
	{
		get => VisualChildren.FirstOrDefault();

		set
		{
			if (Child != null)
			{
				VisualChildren.Remove(Child);
			}

			if (value != null)
			{
				VisualChildren.Add(value);
			}
		}
	}

	#endregion

	#region Methods

	public void AddChild(Visual v)
	{
		VisualChildren.Add(v);
	}

	public void AddChildren(IEnumerable<Visual> v)
	{
		VisualChildren.AddRange(v);
	}

	public void ClearChildren()
	{
		VisualChildren.Clear();
	}

	public void RemoveChild(Visual v)
	{
		VisualChildren.Remove(v);
	}

	#endregion
}