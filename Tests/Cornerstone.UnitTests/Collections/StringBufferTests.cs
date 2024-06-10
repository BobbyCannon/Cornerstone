#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Testing;
using Cornerstone.Text;
using Cornerstone.Text.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

/// <summary>
/// These test are for <seealso cref="IStringBuffer" /> interface.
/// </summary>
/// <remarks>
/// See <seealso cref="BufferTests" /> for testing the <see cref="IBuffer{T}" /> interface.
/// </remarks>
[TestClass]
public class StringBufferTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetString()
	{
		ForEachBuffers(x =>
			{
				var actual = x.SubString(2, 3);
				AreEqual("cde", actual);

				actual = x.SubString(5, 1);
				AreEqual("f", actual);

				ExpectedException<IndexOutOfRangeException>(
					() => x.SubString(6, 1),
					Babel.Tower[BabelKeys.IndexOutOfRange]
				);
			},
			"abcdef"
		);
	}

	[TestMethod]
	public void IndexOfAny()
	{
		ForEachBuffers(x =>
			{
				AreEqual(3, x.IndexOfAny(['\r'], 2, 2));
				AreEqual(3, x.IndexOfAny(['\r'], 3, 5));
				AreEqual(3, x.IndexOfAny(['\r']));

				AreEqual(-1, x.IndexOfAny([' '], 7, 2));
				AreEqual(-1, x.IndexOfAny([' '], 0, 9));
			},
			//123 4 567
			"abc\r\ndef"
		);
	}

	[TestMethod]
	public void IndexOfAnyReverse()
	{
		ForEachBuffers(x =>
			{
				AreEqual(3, x.IndexOfAnyReverse(['\r'], 7));
				AreEqual(3, x.IndexOfAnyReverse(['\r'], 3));
				AreEqual(3, x.IndexOfAnyReverse(['\r']));

				AreEqual(-1, x.IndexOfAnyReverse([' '], 8));
				AreEqual(-1, x.IndexOfAnyReverse([' '], -1));
			},
			//123 4 567
			"abc\r\ndef"
		);
		
		ForEachBuffers(x =>
			{
				AreEqual(-1, x.IndexOfAnyReverse(['\r'], 7));
				AreEqual(-1, x.IndexOfAnyReverse(['\r'], 3));
				AreEqual(-1, x.IndexOfAnyReverse(['\r']));
			},
			string.Empty
		);
	}

	private void ForEachBuffers(Action<IStringBuffer> action, string value)
	{
		ForEachBuffers(action, value.ToCharArray());
	}

	private void ForEachBuffers(Action<IStringBuffer> action, params char[] value)
	{
		var buffers = GetBuffers(value);
		foreach (var buffer in buffers)
		{
			buffer.GetType().FullName.Dump();
			action(buffer);
		}
	}

	private IEnumerable<IStringBuffer> GetBuffers(params char[] values)
	{
		yield return new StringGapBuffer(values);
		yield return new StringRopeBuffer(values);
	}

	#endregion
}