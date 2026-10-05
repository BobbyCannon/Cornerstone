#region References

using System;

#endregion

namespace Cornerstone.Sample;

public class SampleSearchItem
{
	#region Constructors

	public SampleSearchItem(string title, Type hubType)
	{
		Title = title;
		HubType = hubType;
	}

	#endregion

	#region Properties

	public Type HubType { get; }

	public string Title { get; }

	#endregion

	#region Methods

	public override string ToString()
	{
		return Title;
	}

	#endregion
}
