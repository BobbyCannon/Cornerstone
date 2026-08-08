#region References

using System;

#endregion

namespace Cornerstone.PowerShell.Documentation
{
	public class CmdletDescriptionAttribute : Attribute
	{
		#region Constructors

		public CmdletDescriptionAttribute(string desc)
		{
			Description = desc;
			Synopsis = desc;
		}

		public CmdletDescriptionAttribute(string synopsis, string desc)
		{
			Description = desc;
			Synopsis = synopsis;
		}

		#endregion

		#region Properties

		public string Description { get; set; }

		public string Synopsis { get; set; }

		#endregion
	}
}