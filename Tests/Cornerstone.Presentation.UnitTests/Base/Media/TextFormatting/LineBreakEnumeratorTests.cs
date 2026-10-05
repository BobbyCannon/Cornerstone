#region References

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

[TestClass]
public class LineBreakEnumeratorTests
{
	#region Fields

	private readonly ITestLog _outputHelper;

	#endregion

	#region Constructors

	public LineBreakEnumeratorTests()
	{
		_outputHelper = new NullTestLog();
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void BasicLatinTest()
	{
		var lineBreaker = new LineBreakEnumerator("Hello World\r\nThis is a test.");
		LineBreak lineBreak;

		CornerstoneTest.IsTrue(lineBreaker.MoveNext(out lineBreak));
		CornerstoneTest.AreEqual(6, lineBreak.PositionWrap);
		CornerstoneTest.IsFalse(lineBreak.Required);

		CornerstoneTest.IsTrue(lineBreaker.MoveNext(out lineBreak));
		CornerstoneTest.AreEqual(13, lineBreak.PositionWrap);
		CornerstoneTest.IsTrue(lineBreak.Required);

		CornerstoneTest.IsTrue(lineBreaker.MoveNext(out lineBreak));
		CornerstoneTest.AreEqual(18, lineBreak.PositionWrap);
		CornerstoneTest.IsFalse(lineBreak.Required);

		CornerstoneTest.IsTrue(lineBreaker.MoveNext(out lineBreak));
		CornerstoneTest.AreEqual(21, lineBreak.PositionWrap);
		CornerstoneTest.IsFalse(lineBreak.Required);

		CornerstoneTest.IsTrue(lineBreaker.MoveNext(out lineBreak));
		CornerstoneTest.AreEqual(23, lineBreak.PositionWrap);
		CornerstoneTest.IsFalse(lineBreak.Required);

		CornerstoneTest.IsTrue(lineBreaker.MoveNext(out lineBreak));
		CornerstoneTest.AreEqual(28, lineBreak.PositionWrap);
		CornerstoneTest.IsFalse(lineBreak.Required);

		CornerstoneTest.IsFalse(lineBreaker.MoveNext(out lineBreak));
	}

	[PresentationTestMethod]
	public void ForwardTextWithOuterWhitespace()
	{
		var lineBreaker = new LineBreakEnumerator(" Apples Pears Bananas   ");
		var positionsF = GetBreaks(lineBreaker);
		CornerstoneTest.AreEqual(1, positionsF[0].PositionWrap);
		CornerstoneTest.AreEqual(0, positionsF[0].PositionMeasure);
		CornerstoneTest.AreEqual(8, positionsF[1].PositionWrap);
		CornerstoneTest.AreEqual(7, positionsF[1].PositionMeasure);
		CornerstoneTest.AreEqual(14, positionsF[2].PositionWrap);
		CornerstoneTest.AreEqual(13, positionsF[2].PositionMeasure);
		CornerstoneTest.AreEqual(24, positionsF[3].PositionWrap);
		CornerstoneTest.AreEqual(21, positionsF[3].PositionMeasure);
	}

	[ClassDataSource(typeof(LineBreakTestDataGenerator))]
	[PresentationTestMethod(Skip = "Only run when we update Unicode data.")]
	public void ShouldFindBreaks(int lineNumber, int[] codePoints, int[] breakPoints, string rules)
	{
		var text = string.Join(null, codePoints.Select(char.ConvertFromUtf32));

		var lineBreaker = new LineBreakEnumerator(text);

		var foundBreaks = new List<int>();

		while (lineBreaker.MoveNext(out var lineBreak))
		{
			foundBreaks.Add(lineBreak.PositionWrap);
		}

		// Check the same
		var pass = true;

		if (foundBreaks.Count != breakPoints.Length)
		{
			pass = false;
		}
		else
		{
			for (var i = 0; i < foundBreaks.Count; i++)
			{
				if (foundBreaks[i] != breakPoints[i])
				{
					pass = false;
				}
			}
		}

		if (!pass)
		{
			_outputHelper.WriteLine($"Failed test on line {lineNumber}");
			_outputHelper.WriteLine("");
			_outputHelper.WriteLine($"    Code Points: {string.Join(" ", codePoints)}");
			_outputHelper.WriteLine($"Expected Breaks: {string.Join(" ", breakPoints)}");
			_outputHelper.WriteLine($"  Actual Breaks: {string.Join(" ", foundBreaks)}");
			_outputHelper.WriteLine($"           Text: {text}");
			_outputHelper.WriteLine($"     Char Props: {string.Join(" ", codePoints.Select(x => new Codepoint((uint) x).LineBreakClass))}");
			_outputHelper.WriteLine($"     Rules: {rules}");
			_outputHelper.WriteLine("");
		}

		CornerstoneTest.IsTrue(pass);
	}

	[DataRow("Hello\nWorld", 5, 6)]
	[DataRow("Hello\rWorld", 5, 6)]
	[DataRow("Hello\r\nWorld", 5, 7)]
	[PresentationTestMethod]
	public void ShouldFindMandatoryBreaks(string text, int positionMeasure, int positionWrap)
	{
		var lineBreaker = new LineBreakEnumerator(text);

		var breaks = GetBreaks(lineBreaker);

		CornerstoneTest.AreEqual(2, breaks.Count);

		var firstBreak = breaks[0];

		CornerstoneTest.IsTrue(firstBreak.Required);

		CornerstoneTest.AreEqual(positionMeasure, firstBreak.PositionMeasure);

		CornerstoneTest.AreEqual(positionWrap, firstBreak.PositionWrap);
	}

	[PresentationTestMethod]
	public void ShouldHandleEmptyString()
	{
		var lineBreaker = new LineBreakEnumerator(string.Empty);

		CornerstoneTest.IsFalse(lineBreaker.MoveNext(out _));
	}

	private static List<LineBreak> GetBreaks(LineBreakEnumerator lineBreaker)
	{
		var breaks = new List<LineBreak>();

		while (lineBreaker.MoveNext(out var lineBreak))
		{
			breaks.Add(lineBreak);
		}

		return breaks;
	}

	#endregion

	#region Classes

	public class LineBreakTestDataGenerator : IEnumerable<object[]>
	{
		#region Fields

		private readonly List<object[]> _testData;

		#endregion

		#region Constructors

		public LineBreakTestDataGenerator()
		{
			_testData = GenerateTestData();
		}

		#endregion

		#region Methods

		public IEnumerator<object[]> GetEnumerator()
		{
			return _testData.GetEnumerator();
		}

		public static (int[], int[]) ReadLineData(string line)
		{
			var codePoints = new List<int>();
			var breakPoints = new List<int>();

			// Parse the test
			var p = 0;

			while (p < line.Length)
			{
				// Ignore white space
				if (char.IsWhiteSpace(line[p]))
				{
					p++;
					continue;
				}

				if (line[p] == '×')
				{
					p++;
					continue;
				}

				if (line[p] == '÷')
				{
					breakPoints.Add(codePoints.Select(x => x > ushort.MaxValue ? 2 : 1).Sum());
					p++;
					continue;
				}

				var codePointPos = p;

				while ((p < line.Length) && IsHexDigit(line[p]))
				{
					p++;
				}

				var codePointStr = line.Substring(codePointPos, p - codePointPos);
				var codePoint = System.Convert.ToInt32(codePointStr, 16);
				codePoints.Add(codePoint);
			}

			return (codePoints.ToArray(), breakPoints.ToArray());
		}

		private static List<object[]> GenerateTestData()
		{
			// Process each line
			var tests = new List<object[]>();

			// Read the test file
			var url = Path.Combine(UnicodeDataSource.Ucd, "auxiliary/LineBreakTest.txt");

			using (var client = new HttpClient())
			using (var result = client.GetAsync(url).GetAwaiter().GetResult())
			{
				if (!result.IsSuccessStatusCode)
				{
					return tests;
				}

				using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
				using (var reader = new StreamReader(stream))
				{
					var lineNumber = 1;

					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();

						if (line is null)
						{
							break;
						}

						// Get the line, remove comments
						var segments = line.Split('#');

						// Ignore blank/comment only lines
						if (string.IsNullOrWhiteSpace(segments[0]))
						{
							lineNumber++;
							continue;
						}

						var lineData = ReadLineData(segments[0].Trim());

						tests.Add([lineNumber, lineData.Item1, lineData.Item2, segments[1]]);

						lineNumber++;
					}
				}
			}

			return tests;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private static bool IsHexDigit(char ch)
		{
			return char.IsDigit(ch) || ((ch >= 'A') && (ch <= 'F')) || ((ch >= 'a') && (ch <= 'f'));
		}

		#endregion
	}

	#endregion
}