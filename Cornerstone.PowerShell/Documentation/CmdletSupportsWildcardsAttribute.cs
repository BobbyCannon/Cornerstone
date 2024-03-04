#region References

using System;

#endregion

namespace Cornerstone.PowerShell.Documentation
{
	[AttributeUsage(AttributeTargets.Property)]
	public class CmdletSupportsWildcardsAttribute : Attribute
	{
	}
}