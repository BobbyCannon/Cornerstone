#region References

using System;
using System.Collections.Generic;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Serilog;
using VSLangProj;

#endregion

namespace Cornerstone.VisualStudio.Services;

// VS API requires this code to run on Main Thread, so we have to fetch that ahead.
internal class VsProjectAssembliesProvider : IAssemblyProvider
{
	#region Fields

	private readonly List<string> _references;

	#endregion

	#region Constructors

	private VsProjectAssembliesProvider(List<string> references)
	{
		_references = references;
	}

	#endregion

	#region Methods

	public IEnumerable<string> GetAssemblies()
	{
		return _references;
	}

	public static VsProjectAssembliesProvider TryCreate(Project project, string xamlPrimaryAssemblyPath)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		try
		{
			if (project.Object is VSProject vsProject)
			{
				var references = new List<string>(200);
				references.Add(xamlPrimaryAssemblyPath);

				foreach (Reference reference in vsProject.References)
				{
					if ((reference.Type == prjReferenceType.prjReferenceTypeAssembly)
						&& reference.Path is not null)
					{
						references.Add(reference.Path);
					}
				}

				// Not sure if it's possible, but never know what surprise VS has.
				if (references.Count == 1)
				{
					return null;
				}

				return new VsProjectAssembliesProvider(references);
			}
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "VsProjectAssembliesProvider.TryCreate failed with an exception.");
		}
		return null;
	}

	#endregion
}