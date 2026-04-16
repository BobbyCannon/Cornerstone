#region References

using System;

#endregion

namespace Cornerstone.Storage.Sql;

public static class ConnectionStringParser
{
	#region Methods

	public static string GetDatabaseName(string connectionString)
	{
		// Very fast manual parse - case-insensitive and handles common synonyms
		var span = connectionString.AsSpan();

		while (!span.IsEmpty)
		{
			var semiIndex = span.IndexOf(';');
			var segment = semiIndex >= 0 ? span[..semiIndex] : span;
			span = semiIndex >= 0 ? span[(semiIndex + 1)..] : default;

			var eqIndex = segment.IndexOf('=');
			if (eqIndex <= 0)
			{
				continue;
			}

			var keySpan = segment[..eqIndex].Trim();
			var valueSpan = segment[(eqIndex + 1)..].Trim();

			// Check for common database name keys (case-insensitive)
			if (keySpan.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase)
				|| keySpan.Equals("Database", StringComparison.OrdinalIgnoreCase)
				|| keySpan.Equals("Data Source", StringComparison.OrdinalIgnoreCase))
			{
				return valueSpan.ToString();
			}
		}

		return null;
	}

	public static string GetMasterString(string connectionString)
	{
		if (string.IsNullOrEmpty(connectionString))
		{
			return "Database=master;";
		}

		var span = connectionString.AsSpan();
		if (span.Contains("master", StringComparison.OrdinalIgnoreCase)
			&& (span.Contains("database=master", StringComparison.OrdinalIgnoreCase)
				|| span.Contains("initial catalog=master", StringComparison.OrdinalIgnoreCase)))
		{
			return connectionString;
		}

		var pos = span.IndexOf("database=", StringComparison.OrdinalIgnoreCase);
		if (pos == -1)
		{
			pos = span.IndexOf("initial catalog=", StringComparison.OrdinalIgnoreCase);
		}

		if (pos == -1)
		{
			return connectionString + (connectionString.EndsWith(';') ? "" : ";") + "Database=master;";
		}

		// Simple replace from = to next ; 
		var eq = span[pos..].IndexOf('=') + pos + 1;
		var semi = span[eq..].IndexOf(';');
		semi = semi == -1 ? span.Length : semi + eq;

		return string.Concat(connectionString.AsSpan(0, eq), "master", connectionString.AsSpan(semi));
	}

	#endregion
}