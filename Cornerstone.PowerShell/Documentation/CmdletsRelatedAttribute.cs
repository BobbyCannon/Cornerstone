#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.PowerShell.Documentation
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CmdletsRelatedAttribute : Attribute
	{
		#region Constructors

		public CmdletsRelatedAttribute(params Type[] type)
		{
			var cmdlets = new List<Type>();
			foreach (var t in type)
			{
				cmdlets.Add(t);
			}

			RelatedCmdlets = cmdlets;
		}

		#endregion

		#region Properties

		public string[] ExternalCmdlets { get; set; }
		public List<Type> RelatedCmdlets { get; set; }

		#endregion
	}
}