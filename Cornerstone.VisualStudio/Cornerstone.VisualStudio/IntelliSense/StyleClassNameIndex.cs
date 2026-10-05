#region References

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// Caches style class names from CXAML/AXAML/XAML in the current solution so
/// Classes="" can complete theme classes defined outside the current document
/// (e.g. ^.Search in TextBox.cxaml).
/// </summary>
[Export]
internal sealed class StyleClassNameIndex
{
	#region Fields

	private readonly object _gate;
	private readonly Dictionary<string, IReadOnlyList<string>> _namesByPath;
	private string[] _names;
	private bool _scanQueued;
	private readonly VisualStudioWorkspace _workspace;

	#endregion

	#region Constructors

	[ImportingConstructor]
	public StyleClassNameIndex([Import(AllowDefault = true)] VisualStudioWorkspace workspace)
	{
		_gate = new object();
		_namesByPath = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		_names = [];
		_scanQueued = false;
		_workspace = workspace;
		if (_workspace != null)
		{
			_workspace.WorkspaceChanged += OnWorkspaceChanged;
		}

		QueueScan();
	}

	#endregion

	#region Methods

	public IReadOnlyList<string> GetNames()
	{
		lock (_gate)
		{
			return _names;
		}
	}

	private static bool IsXamlDocument(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}

		return path.EndsWith(".cxaml", StringComparison.OrdinalIgnoreCase) ||
			path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase);
	}

	private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
	{
		if ((e.Kind != WorkspaceChangeKind.DocumentChanged) &&
			(e.Kind != WorkspaceChangeKind.DocumentAdded) &&
			(e.Kind != WorkspaceChangeKind.DocumentRemoved) &&
			(e.Kind != WorkspaceChangeKind.AdditionalDocumentChanged) &&
			(e.Kind != WorkspaceChangeKind.AdditionalDocumentAdded) &&
			(e.Kind != WorkspaceChangeKind.AdditionalDocumentRemoved) &&
			(e.Kind != WorkspaceChangeKind.SolutionChanged) &&
			(e.Kind != WorkspaceChangeKind.SolutionAdded) &&
			(e.Kind != WorkspaceChangeKind.ProjectAdded) &&
			(e.Kind != WorkspaceChangeKind.ProjectRemoved))
		{
			return;
		}

		if ((e.DocumentId != null) &&
			((e.Kind == WorkspaceChangeKind.DocumentChanged) ||
				(e.Kind == WorkspaceChangeKind.DocumentAdded) ||
				(e.Kind == WorkspaceChangeKind.DocumentRemoved) ||
				(e.Kind == WorkspaceChangeKind.AdditionalDocumentChanged) ||
				(e.Kind == WorkspaceChangeKind.AdditionalDocumentAdded) ||
				(e.Kind == WorkspaceChangeKind.AdditionalDocumentRemoved)))
		{
			var path = e.NewSolution.GetDocument(e.DocumentId)?.FilePath
				?? e.OldSolution.GetDocument(e.DocumentId)?.FilePath
				?? e.NewSolution.GetAdditionalDocument(e.DocumentId)?.FilePath
				?? e.OldSolution.GetAdditionalDocument(e.DocumentId)?.FilePath;
			if (!IsXamlDocument(path))
			{
				return;
			}
		}

		QueueScan();
	}

	private void QueueScan()
	{
		lock (_gate)
		{
			if (_scanQueued)
			{
				return;
			}

			_scanQueued = true;
		}

		ScanAsync().FireAndForget();
	}

	private async Task ScanAsync()
	{
		try
		{
			var workspace = _workspace;
			if (workspace == null)
			{
				return;
			}

			var byPath = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var project in workspace.CurrentSolution.Projects)
			{
				foreach (var document in EnumerateXamlDocuments(project))
				{
					if (string.IsNullOrEmpty(document.FilePath))
					{
						continue;
					}

					var text = await document.GetTextAsync().ConfigureAwait(false);
					var names = StyleClassScanner.FindClassNames(text.ToString());
					if (names.Count > 0)
					{
						byPath[document.FilePath] = names;
					}
				}
			}

			var merged = Merge(byPath);
			lock (_gate)
			{
				_namesByPath.Clear();
				foreach (var pair in byPath)
				{
					_namesByPath[pair.Key] = pair.Value;
				}

				_names = merged;
				_scanQueued = false;
			}
		}
		catch
		{
			lock (_gate)
			{
				_scanQueued = false;
			}
		}
	}

	private static string[] Merge(Dictionary<string, IReadOnlyList<string>> byPath)
	{
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var names = new List<string>();
		foreach (var pair in byPath)
		{
			foreach (var name in pair.Value)
			{
				if (seen.Add(name))
				{
					names.Add(name);
				}
			}
		}

		return names.ToArray();
	}

	private static IEnumerable<TextDocument> EnumerateXamlDocuments(Project project)
	{
		foreach (var document in project.AdditionalDocuments)
		{
			if (IsXamlDocument(document.FilePath))
			{
				yield return document;
			}
		}

		foreach (var document in project.Documents)
		{
			if (IsXamlDocument(document.FilePath))
			{
				yield return document;
			}
		}
	}

	#endregion
}
