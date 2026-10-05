#region References

using System;
using Cornerstone.Automation;

#endregion

namespace Cornerstone.AutomationTests;

/// <summary>
/// In-memory element host used by automation unit tests.
/// </summary>
public class TestElementHost : ElementHost
{
	#region Constructors

	public TestElementHost()
		: base(null, null)
	{
		SearchTimeout = TimeSpan.FromMilliseconds(200);
	}

	#endregion

	#region Properties

	public override Element FocusedElement => FirstOrDefault(x => x.Focused, wait: false);

	public override string Id => "host";

	public int RefreshCount { get; private set; }

	public TimeSpan SearchTimeout { get; set; }

	protected override int SearchTimeoutMilliseconds => (int) SearchTimeout.TotalMilliseconds;

	#endregion

	#region Methods

	public override ElementHost Refresh<T>(Func<T, bool> condition)
	{
		RefreshCount++;
		return this;
	}

	public override ElementHost WaitForComplete(int minimumDelay = 0)
	{
		return this;
	}

	protected override void Dispose(bool disposing)
	{
	}

	#endregion
}
