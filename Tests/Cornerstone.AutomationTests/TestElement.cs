#region References

using System;
using System.Drawing;
using Cornerstone.Automation;

#endregion

namespace Cornerstone.AutomationTests;

/// <summary>
/// In-memory element used by automation unit tests.
/// </summary>
public class TestElement : Element
{
	#region Constructors

	public TestElement(string id, ElementHost parent = null, string name = null, string automationId = null)
		: base(parent?.Application, parent)
	{
		Id = id;
		Name = name ?? id;
		AutomationId = automationId ?? id;
	}

	#endregion

	#region Properties

	public override string AutomationId { get; }

	public override bool Enabled => true;

	public override bool Focused => false;

	public override Element FocusedElement => FirstOrDefault(x => x.Focused, wait: false);

	public override int Height => 0;

	public override string Id { get; }

	public override string this[string id]
	{
		get => string.Empty;
		set { }
	}

	public override Point Location => Point.Empty;

	public override string Name { get; }

	public override int Width => 0;

	#endregion

	#region Methods

	public override Element Click(int x = 0, int y = 0, bool refresh = true)
	{
		return this;
	}

	public override Element Focus()
	{
		return this;
	}

	public override Element LeftClick(int x = 0, int y = 0)
	{
		return this;
	}

	public override Element MiddleClick(int x = 0, int y = 0)
	{
		return this;
	}

	public override Element MoveMouseTo(int x = 0, int y = 0)
	{
		return this;
	}

	public override ElementHost Refresh<T>(Func<T, bool> condition)
	{
		return this;
	}

	public override Element RightClick(int x = 0, int y = 0)
	{
		return this;
	}

	public override string ToDetailString()
	{
		return Id;
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
