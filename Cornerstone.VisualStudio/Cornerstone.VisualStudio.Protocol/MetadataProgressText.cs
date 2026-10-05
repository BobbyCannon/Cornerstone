using System;

namespace Cornerstone.VisualStudio.Protocol
{
	/// <summary>
	/// The one-line status shown while completion metadata is loading.
	/// </summary>
	public static class MetadataProgressText
	{
		public static string Format(string phase, string name, int index, int total)
		{
			var label = name ?? string.Empty;
			return index + " of " + total + " - " + Verb(phase) + " " + label;
		}

		private static string Verb(string phase)
		{
			if (string.Equals(phase, "Checking", StringComparison.Ordinal))
			{
				return "Checking cache of";
			}

			if (string.Equals(phase, "Converting", StringComparison.Ordinal))
			{
				return "Converting metadata of";
			}

			if (string.Equals(phase, "Writing", StringComparison.Ordinal))
			{
				return "Writing metadata of";
			}

			return "Reading metadata of";
		}
	}
}