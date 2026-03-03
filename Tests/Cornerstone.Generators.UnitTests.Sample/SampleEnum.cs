#region References

using System.ComponentModel.DataAnnotations;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Generators.UnitTests.Sample;

[SourceReflection]
public enum SampleEnum
{
	[Display(Name = "Unknown", ShortName = "-1", Order = 0)]
	Unknown = 0,

	[Display(Name = "One", ShortName = "1", Order = 1)]
	One = 1,

	[Display(Name = "One", ShortName = "2", Order = 2)]
	Two = 2,

	[Display(Name = "Ten", ShortName = "10", Order = 3)]
	Ten = 10
}