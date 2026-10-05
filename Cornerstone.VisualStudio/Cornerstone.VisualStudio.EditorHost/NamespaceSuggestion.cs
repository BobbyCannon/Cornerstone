#region References

using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Protocol;

#endregion

namespace Cornerstone.VisualStudio.EditorHost;

/// <summary>
/// Decides which xmlns lightbulb applies for a type name, using cached metadata.
/// </summary>
internal static class NamespaceSuggestion
{
	#region Methods

	public static void Find(
		Metadata metadata,
		string typeName,
		string documentText,
		bool hasAlias,
		string existingAlias,
		out int kind,
		out string xmlNamespace,
		out string alias)
	{
		kind = LookupNamespaceResponseMessage.KindNone;
		xmlNamespace = string.Empty;
		alias = string.Empty;
		if ((metadata == null) || string.IsNullOrEmpty(typeName) || (metadata.InverseNamespace == null))
		{
			return;
		}

		string foundNamespace = null;
		foreach (var pair in metadata.InverseNamespace)
		{
			var name = pair.Key ?? string.Empty;
			var dot = name.LastIndexOf('.');
			var shortName = dot < 0 ? name : name.Substring(dot + 1);
			if (shortName == typeName)
			{
				foundNamespace = pair.Value;
				break;
			}
		}

		if (string.IsNullOrEmpty(foundNamespace) || metadata.IsTypeInDefaultXmlns(typeName))
		{
			return;
		}

		var aliases = CompletionEngine.GetNamespaceAliases(documentText ?? string.Empty);
		var alreadyMapped = (aliases != null) && aliases.ContainsValue(foundNamespace);
		xmlNamespace = foundNamespace;
		if (!alreadyMapped)
		{
			if (!hasAlias)
			{
				kind = LookupNamespaceResponseMessage.KindAddNamespaceAndAlias;
				alias = PrefixFromNamespace(foundNamespace);
			}
			else
			{
				kind = LookupNamespaceResponseMessage.KindAddNamespace;
				alias = existingAlias ?? string.Empty;
			}

			return;
		}

		if (!hasAlias)
		{
			kind = LookupNamespaceResponseMessage.KindUseAlias;
			alias = PrefixFromNamespace(foundNamespace);
		}
	}

	private static string PrefixFromNamespace(string xmlNamespace)
	{
		if (string.IsNullOrEmpty(xmlNamespace))
		{
			return string.Empty;
		}

		var colon = xmlNamespace.Split(':');
		var tail = colon[colon.Length - 1];
		var dot = tail.Split('.');
		return dot[dot.Length - 1];
	}

	#endregion
}
