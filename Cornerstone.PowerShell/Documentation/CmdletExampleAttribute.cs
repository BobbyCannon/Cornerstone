#region References

using System;

#endregion

namespace Cornerstone.PowerShell.Documentation
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class CmdletExampleAttribute : Attribute
	{
		#region Properties

		public string Code { get; set; }

		public int Order { get; set; }

		public string Remarks { get; set; }

		#endregion
	}
}