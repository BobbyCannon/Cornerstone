#region References

using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class ProfilerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void StartStop()
	{
		var profiler = GetProfiler();
		IsFalse(profiler.IsRunning);

		profiler.Start("First");
		IsTrue(profiler.IsRunning);
		IncrementTime(seconds: 1);
		profiler.Start("Child");
		IsTrue(profiler.IsRunning);
		IncrementTime(seconds: 2);
		profiler.Start("GrandChild");
		IsTrue(profiler.IsRunning);
		IncrementTime(ticks: 1);
		profiler.Stop();
		IsTrue(profiler.IsRunning);
		profiler.Stop();
		IsTrue(profiler.IsRunning);
		profiler.Stop();

		IsFalse(profiler.IsRunning);

		profiler.Start("Second");
		IncrementTime(seconds: 2);
		IsTrue(profiler.IsRunning);
		profiler.Stop();

		var actual = profiler.ToString();
		var expected = """
						100.00% 0:00:00:05.0000001: Profiler
							 60.00% 0:00:00:03.0000001: First
								 66.67% 0:00:00:02.0000001: Child
									  0.00% 0:00:00:00.0000001: GrandChild
									100.00% 0:00:00:02.0000000: Remainder
								 33.33% 0:00:00:01.0000000: Remainder
							 40.00% 0:00:00:02.0000000: Second

						""";

		AreEqual(expected, actual);
		IsFalse(profiler.IsRunning);
	}

	[TestMethod]
	public void TimeShouldAppend()
	{
		var profiler = GetProfiler();
		IsFalse(profiler.IsRunning);

		for (var i = 0; i < 10; i++)
		{
			profiler.Time("First", () =>
			{
				profiler.Time("Child", () =>
				{
					IncrementTime(milliseconds: 500);

					profiler.Time("GrandChild", () => { IncrementTime(milliseconds: 500); });
				});
			});
		}

		var actual = profiler.ToString();
		var expected = """
						100.00% 0:00:00:10.0000000: Profiler
							100.00% 0:00:00:10.0000000: First
								100.00% 0:00:00:10.0000000: Child
									 50.00% 0:00:00:05.0000000: GrandChild
									 50.00% 0:00:00:05.0000000: Remainder

						""";

		AreEqual(expected, actual);
	}

	[TestMethod]
	public void TimeShouldNotAppend()
	{
		var profiler = GetProfiler();
		profiler.AppendToExistingTimers = false;

		IsFalse(profiler.IsRunning);

		profiler.Time("First", () =>
		{
			profiler.Time("Child", () =>
			{
				IncrementTime(milliseconds: 500);

				profiler.Time("GrandChild", () => { IncrementTime(milliseconds: 500); });
			});
		});

		profiler.Time("First", () => { IncrementTime(seconds: 1, milliseconds: 123); });

		var actual = profiler.ToString();
		var expected = """
						100.00% 0:00:00:02.1230000: Profiler
							 47.10% 0:00:00:01.0000000: First
								100.00% 0:00:00:01.0000000: Child
									 50.00% 0:00:00:00.5000000: GrandChild
									 50.00% 0:00:00:00.5000000: Remainder
							 52.90% 0:00:00:01.1230000: First

						""";

		AreEqual(expected, actual);
	}

	#endregion
}