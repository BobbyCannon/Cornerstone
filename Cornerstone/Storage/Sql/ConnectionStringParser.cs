#region References

using System;

#endregion

namespace Cornerstone.Storage.Sql;

public static class ConnectionStringParser
{
	#region Methods

	public static string GetDatabaseName(string connectionString)
	{
		if (string.IsNullOrEmpty(connectionString))
		{
			return null;
		}

		string initialCatalog = null;
		string database = null;
		string attachDbFilename = null;
		string dataSource = null;
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
			var value = UnwrapValue(segment[(eqIndex + 1)..].Trim());

			if (keySpan.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase))
			{
				initialCatalog = value;
			}
			else if (keySpan.Equals("Database", StringComparison.OrdinalIgnoreCase))
			{
				database = value;
			}
			else if (keySpan.Equals("AttachDbFilename", StringComparison.OrdinalIgnoreCase))
			{
				attachDbFilename = value;
			}
			else if (keySpan.Equals("Data Source", StringComparison.OrdinalIgnoreCase)
					|| keySpan.Equals("Filename", StringComparison.OrdinalIgnoreCase))
			{
				dataSource = value;
			}
		}

		if (!string.IsNullOrWhiteSpace(initialCatalog))
		{
			return initialCatalog;
		}

		if (!string.IsNullOrWhiteSpace(database))
		{
			return database;
		}

		if (!string.IsNullOrWhiteSpace(attachDbFilename))
		{
			return attachDbFilename;
		}

		return string.IsNullOrWhiteSpace(dataSource) ? null : dataSource;
	}

	public static string GetMasterString(string connectionString)
	{
		if (string.IsNullOrEmpty(connectionString))
		{
			return "Database=master;";
		}

		var span = connectionString.AsSpan();
		if (HasCatalogValue(span, "master"))
		{
			return connectionString;
		}

		var pos = IndexOfKey(span, "database=");
		if (pos == -1)
		{
			pos = IndexOfKey(span, "initial catalog=");
		}

		if (pos == -1)
		{
			return connectionString + (connectionString.EndsWith(';') ? "" : ";") + "Database=master;";
		}

		var eq = span[pos..].IndexOf('=') + pos + 1;
		var semi = span[eq..].IndexOf(';');
		semi = semi == -1 ? span.Length : semi + eq;

		return string.Concat(connectionString.AsSpan(0, eq), "master", connectionString.AsSpan(semi));
	}

	private static bool ContainsKeyValue(ReadOnlySpan<char> span, string keyEquals, string expectedValue)
	{
		var pos = IndexOfKey(span, keyEquals);
		if (pos == -1)
		{
			return false;
		}

		var eq = span[pos..].IndexOf('=') + pos + 1;
		var semi = span[eq..].IndexOf(';');
		semi = semi == -1 ? span.Length : semi + eq;
		var value = UnwrapValue(span[eq..semi].Trim());
		return value.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
	}

	private static bool HasCatalogValue(ReadOnlySpan<char> span, string catalog)
	{
		return ContainsKeyValue(span, "database=", catalog)
			|| ContainsKeyValue(span, "initial catalog=", catalog);
	}

	private static int IndexOfKey(ReadOnlySpan<char> span, string keyEquals)
	{
		return span.IndexOf(keyEquals, StringComparison.OrdinalIgnoreCase);
	}

	private static string UnwrapValue(ReadOnlySpan<char> value)
	{
		if ((value.Length >= 2)
			&& (((value[0] == '"') && (value[^1] == '"'))
				|| ((value[0] == '\'') && (value[^1] == '\''))))
		{
			return value[1..^1].ToString();
		}

		return value.ToString();
	}

	#endregion
}