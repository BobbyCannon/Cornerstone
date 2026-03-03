#region References

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#endregion

namespace Cornerstone.Generators.UnitTests;

public sealed class InMemoryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
	#region Fields

	private static readonly AnalyzerConfigOptions _emptyOptions;
	private readonly InMemoryGlobalAnalyzerConfigOptions _globalOptions;

	#endregion

	#region Constructors

	public InMemoryAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> globalOptions)
	{
		_globalOptions = new InMemoryGlobalAnalyzerConfigOptions(globalOptions);
	}

	static InMemoryAnalyzerConfigOptionsProvider()
	{
		_emptyOptions = new EmptyAnalyzerConfigOptions();
	}

	#endregion

	#region Properties

	public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

	#endregion

	#region Methods

	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
	{
		return _emptyOptions;
	}

	public override AnalyzerConfigOptions GetOptions(AdditionalText text)
	{
		return _emptyOptions;
	}

	#endregion

	#region Classes

	internal sealed class EmptyAnalyzerConfigOptions : AnalyzerConfigOptions
	{
		#region Methods

		public override bool TryGetValue(string key, out string value)
		{
			value = null!;
			return false;
		}

		#endregion
	}

	#endregion
}