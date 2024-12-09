#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

#endregion

namespace Cornerstone.Parsers.CSharp;

public class CSharpTokenizer
	: Tokenizer<CSharpTokenData, SyntaxKind>
{
	#region Methods

	/// <inheritdoc />
	public override IEnumerable<CSharpTokenData> GetTokens()
	{
		var options = CSharpParseOptions.Default;
		options = options.WithKind(SourceCodeKind.Script);

		var code = ToString();
		var tokens = CSharpSyntaxTree.ParseText(code, options);
		var root = tokens.GetRoot();

		var tokens1 = root
			.DescendantTokens(descendIntoTrivia: true)
			.Where(x => x.RawKind
				is not (int)SyntaxKind.EndOfFileToken
				and not (int)SyntaxKind.EndOfDirectiveToken
			)
			.Select(Convert)
			.ToList();

		var tokens2 = root
			.DescendantTrivia(descendIntoTrivia: true)
			.Select(Convert)
			.ToList();

		var tokens3 = root
			.DescendantNodes(descendIntoTrivia: true, descendIntoChildren: _ => true)
			.Where(x => x.RawKind
				is not (int) SyntaxKind.FileScopedNamespaceDeclaration
				and not (int) SyntaxKind.ClassDeclaration
				and not (int) SyntaxKind.MethodDeclaration
				and not (int) SyntaxKind.Block
				and not (int) SyntaxKind.BaseList
				and not (int) SyntaxKind.ArgumentList
				and not (int) SyntaxKind.ParameterList
				and not (int) SyntaxKind.ExpressionStatement
				and not (int) SyntaxKind.ReturnStatement
				and not (int) SyntaxKind.InvocationExpression
			)
			.Select(Convert)
			.ToList();

		var response = tokens1
			.Union(tokens2)
			.Union(tokens3)
			.OrderBy(x => x.StartIndex)
			.ThenByDescending(x => x.EndIndex)
			.ToList();

		return ProcessTokens(response);
	}

	/// <inheritdoc />
	public override bool ParseNext()
	{
		return false;
	}

	/// <inheritdoc />
	protected override void ParseString(char quote)
	{
	}

	private CSharpTokenData Convert(SyntaxNode x)
	{
		var response = new CSharpTokenData
		{
			StartIndex = x.SpanStart,
			EndIndex = x.SpanStart + x.Span.Length,
			Type = x.Kind(),
			//Value = x
		};

		response.SetDocument(this);
		return response;
	}

	private CSharpTokenData Convert(SyntaxTrivia x)
	{
		var response = new CSharpTokenData
		{
			StartIndex = x.SpanStart,
			EndIndex = x.SpanStart + x.Span.Length,
			Type = x.Kind(),
			//Value = x
		};

		response.SetDocument(this);
		return response;
	}

	private CSharpTokenData Convert(SyntaxToken x)
	{
		var response = new CSharpTokenData
		{
			StartIndex = x.SpanStart,
			EndIndex = x.SpanStart + x.Span.Length,
			Type = x.Kind(),
			//Value = x
		};

		response.SetDocument(this);
		return response;
	}

	private void ProcessChildToken(CSharpTokenData parent, CSharpTokenData child)
	{
		if (parent.TokenIndexes is { Length: > 0 }
			&& (parent.TokenIndexes[parent.TokenIndexes.Length -1] == child.StartIndex))
		{
			return;
		}

		parent.TokenIndexes = ArrayExtensions.CombineArrays(parent.TokenIndexes ?? [], [child.StartIndex]);
	}

	private IEnumerable<CSharpTokenData> ProcessTokens(List<CSharpTokenData> tokens)
	{
		var response = new List<CSharpTokenData>();
		if (tokens is not { Count: > 0 })
		{
			return response;
		}

		var lastToken = tokens[0];
		response.Add(lastToken);

		for (var index = 1; index < tokens.Count; index++)
		{
			var currentToken = tokens[index];

			if ((currentToken.StartIndex >= lastToken.StartIndex)
				&& (currentToken.StartIndex < lastToken.EndIndex))
			{
				ProcessChildToken(lastToken, currentToken);
				continue;
			}

			response.Add(currentToken);
			lastToken = currentToken;
		}

		return response;
	}

	#endregion
}