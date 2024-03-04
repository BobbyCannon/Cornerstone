#region References

using System;

#endregion

namespace Cornerstone.PowerShell.Documentation
{
	public class CmdletGroupAttribute : Attribute
	{
		#region Constructors

		public CmdletGroupAttribute(string group)
		{
			Group = group;
		}

		#endregion

		#region Properties

		public string Group { get; set; }

		#endregion
	}
}