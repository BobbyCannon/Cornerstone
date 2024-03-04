#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

/// <summary>
/// These test are for <seealso cref="IBuffer{T}" /> interface.
/// </summary>
[TestClass]
public class BufferTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void BoundsCheck()
	{
		ForEachBuffers(x =>
			{
				x.VerifyRange(0);
				x.VerifyRange(4);

				ExpectedException<IndexOutOfRangeException>(
					() => x.VerifyRange(5),
					Babel.Tower[BabelKeys.IndexOutOfRange]
					
				);
			},
			1, 2, 3, 4, 5
		);
	}

	private void ForEachBuffers<T>(Action<IBuffer<T>> action, params T[] value)
	{
		var buffers = GetBuffers(value);
		foreach (var buffer in buffers)
		{
			buffer.GetType().FullName.Dump();
			action(buffer);
		}
	}

	private IEnumerable<IBuffer<T>> GetBuffers<T>(params T[] values)
	{
		yield return new GapBuffer<T>(values);
		yield return new RopeBuffer<T>(values);
	}

	#endregion
}