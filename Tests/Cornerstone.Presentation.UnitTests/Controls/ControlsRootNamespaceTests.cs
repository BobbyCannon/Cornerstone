#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cornerstone.Presentation.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

/// <summary>
/// The root control namespace is closed. New controls go in a kind folder.
/// </summary>
[TestClass]
public class ControlsRootNamespaceTests
{
	#region Methods

	[TestMethod]
	public void RootNamespaceMatchesSnapshot()
	{
		var actual = RootPublicTypeNames();
		var expected = ReadSnapshot();
		var added = Except(actual, expected);
		var removed = Except(expected, actual);
		if ((added.Length == 0) && (removed.Length == 0))
		{
			return;
		}

		Assert.Fail(BuildMessage(added, removed));
	}

	private static void AppendNames(StringBuilder builder, string label, string[] names)
	{
		builder.Append(label);
		builder.Append(" (");
		builder.Append(names.Length);
		builder.AppendLine("):");
		for (var i = 0; i < names.Length; i++)
		{
			builder.Append("  ");
			builder.AppendLine(names[i]);
		}
	}

	private static string BuildMessage(string[] added, string[] removed)
	{
		var builder = new StringBuilder();
		builder.AppendLine("The root namespace Cornerstone.Presentation.Controls changed.");
		builder.AppendLine("Control classes belong in that namespace. Helpers stay in a kind folder.");
		builder.AppendLine("Update ControlsRootNamespaceSnapshot.txt when the root control list changes.");
		AppendNames(builder, "Added", added);
		AppendNames(builder, "Removed", removed);
		return builder.ToString();
	}

	private static string[] Except(string[] left, string[] right)
	{
		var skip = new HashSet<string>(right, StringComparer.Ordinal);
		var result = new List<string>();
		for (var i = 0; i < left.Length; i++)
		{
			if (skip.Add(left[i]))
			{
				result.Add(left[i]);
			}
		}

		return result.ToArray();
	}

	private static string FindSnapshot()
	{
		for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
		{
			var candidate = Path.Combine(dir.FullName, "Controls", "ControlsRootNamespaceSnapshot.txt");
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		throw new FileNotFoundException("ControlsRootNamespaceSnapshot.txt was not found above the test output directory.");
	}

	private static string[] ReadSnapshot()
	{
		var path = FindSnapshot();
		var lines = File.ReadAllLines(path);
		var names = new List<string>();
		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i].Trim();
			if ((line.Length == 0) || line.StartsWith("#"))
			{
				continue;
			}

			names.Add(line);
		}

		names.Sort(StringComparer.Ordinal);
		return names.ToArray();
	}

	private static string[] RootPublicTypeNames()
	{
		var assembly = typeof(Control).Assembly;
		Type[] types;
		try
		{
			types = assembly.GetExportedTypes();
		}
		catch (ReflectionTypeLoadException exception)
		{
			types = exception.Types.Where(type => type != null).ToArray();
		}

		var names = new List<string>();
		for (var i = 0; i < types.Length; i++)
		{
			var type = types[i];
			if ((type == null) || type.IsNested || (type.Namespace != "Cornerstone.Presentation.Controls"))
			{
				continue;
			}

			if (type.Name.IndexOf('<') >= 0)
			{
				continue;
			}

			names.Add(type.Name);
		}

		names.Sort(StringComparer.Ordinal);
		return names.ToArray();
	}

	#endregion
}