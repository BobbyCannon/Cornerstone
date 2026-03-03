#region References

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

#endregion

namespace Cornerstone.Generators.UnitTests;

internal sealed class InMemoryGlobalAnalyzerConfigOptions : AnalyzerConfigOptions
{
	#region Fields

	private readonly IReadOnlyDictionary<string, string> _options;

	#endregion

	#region Constructors

	public InMemoryGlobalAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options)
	{
		_options = options ?? new Dictionary<string, string>();
	}

	#endregion

	#region Methods

	public override bool TryGetValue(string key, out string value)
	{
		return _options.TryGetValue(key, out value);
	}

	#endregion
}