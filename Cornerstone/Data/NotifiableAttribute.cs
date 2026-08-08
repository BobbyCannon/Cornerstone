#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Instruct Cornerstone.Generators to generate the INPC for a set of properties using this backing field
/// </summary>
[SourceReflection]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class NotifiableAttribute : CornerstoneAttribute
{
	#region Constructors

	public NotifiableAttribute(string[] properties)
	{
		Properties = properties;
	}

	#endregion

	#region Properties

	public string[] Properties { get; set; }

	#endregion
}