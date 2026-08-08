#region References

using System;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class GuidConverters
{
	#region Fields

	public static readonly FuncValueConverter<Guid, bool> IsEmpty;
	public static readonly FuncValueConverter<Guid, bool> IsNotEmpty;

	#endregion

	#region Constructors

	static GuidConverters()
	{
		IsEmpty = new(x => x == Guid.Empty);
		IsNotEmpty = new(x => x != Guid.Empty);
	}

	#endregion
}