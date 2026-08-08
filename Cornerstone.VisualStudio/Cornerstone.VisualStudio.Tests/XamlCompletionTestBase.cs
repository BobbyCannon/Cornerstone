#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Core.DnlibMetadataProvider;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests;

public class FolderAssemblyProvider : IAssemblyProvider
{
	#region Fields

	private readonly string _path;

	#endregion

	#region Constructors

	public FolderAssemblyProvider(string path)
	{
		_path = path;
	}

	#endregion

	#region Methods

	public IEnumerable<string> GetAssemblies()
	{
		HashSet<string> result = [_path];

		if (Path.GetDirectoryName(_path) is { } directory)
		{
			// Calculate Reference path
			var segments = directory.Split('\\', '/');
			var avaloniaPathBuilder = new StringBuilder(1024);
			foreach (var segment in segments)
			{
				if (string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase))
				{
					avaloniaPathBuilder.Append("obj");
				}
				else
				{
					avaloniaPathBuilder.Append(segment);
				}
				avaloniaPathBuilder.Append(Path.DirectorySeparatorChar);
			}
			avaloniaPathBuilder.Append("Avalonia");
			avaloniaPathBuilder.Append(Path.DirectorySeparatorChar);

			var referencePath = Path.Combine(avaloniaPathBuilder.ToString(), "references");
			var depsPath = Path.Combine(directory,
				Path.GetFileNameWithoutExtension(_path) + ".deps.json");

			var files = File.Exists(referencePath)
				? File.ReadAllLines(referencePath)
				: File.Exists(depsPath)
					? DepsJsonAssemblyListLoader.ParseFile(depsPath)
					: Directory.GetFiles(directory).Where(f => f.EndsWith(".dll") || f.EndsWith(".exe"));

			foreach (var file in files)
			{
				result.Add(file);
			}
		}
		return result;
	}

	#endregion
}

public class XamlCompletionTestBase
{
	#region Fields

	private static readonly Core.AssemblyMetadata.Metadata Metadata = new MetadataReader(new DnlibMetadataProvider())
		.GetForTargetAssembly(new FolderAssemblyProvider(typeof(XamlCompletionTestBase).Assembly.GetModules()[0].FullyQualifiedName));

	private static readonly string Prologue = @"<UserControl xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006'
        xmlns:local='clr-namespace:CompletionEngineTests.Models;assembly=Cornerstone.VisualStudio.Tests'>".Replace("'", "\"");

	#endregion

	#region Methods

	protected void AssertSingleCompletion(string xaml, string typed, string completion)
	{
		AssertSingleCompletionInMiddleOfText(xaml, "", typed, completion);
	}

	protected void AssertSingleCompletionInMiddleOfText(string xaml, string xamlAfterCursor, string typed, string completion)
	{
		var comp = GetCompletionsFor(xaml + typed, xamlAfterCursor);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.True(xaml.Length == comp.StartPosition, $"Invalid completion start position typed: {typed} expected: {completion}");

		Assert.Contains(comp.Completions, c => c.InsertText == completion);

		Assert.Single(comp.Completions, c => c.InsertText == completion);
	}

	protected CompletionSet GetCompletionsFor(string xaml, string xamlAfterCursor = "")
	{
		xaml = Prologue + xaml;
		var engine = new CompletionEngine();
		var set = engine.GetCompletions(Metadata, xaml + xamlAfterCursor, xaml.Length, Assembly.GetCallingAssembly().GetName().Name);
		return TransformCompletionSet(set);
	}

	private CompletionSet TransformCompletionSet(CompletionSet set)
	{
		if (set == null)
		{
			return null;
		}
		return new CompletionSet
		{
			StartPosition = set.StartPosition - Prologue.Length,
			Completions = set.Completions.Select(c => new Completion(
				c.DisplayText,
				c.InsertText,
				c.Description,
				c.Kind,
				// RecommendedCursorOffset is an index into InsertText, not a document offset.
				c.RecommendedCursorOffset,
				c.Suffix,
				// DeleteTextOffset is document-relative when set — shift like StartPosition.
				c.DeleteTextOffset.HasValue ? c.DeleteTextOffset - Prologue.Length : null,
				c.Priority)
			{
				TriggerCompletionAfterInsert = c.TriggerCompletionAfterInsert
			}).ToList()
		};
	}

	#endregion
}