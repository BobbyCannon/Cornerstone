#region References

using Cornerstone.Data;
using Cornerstone.Parsers.Html;
using Microsoft.CodeAnalysis.CSharp;

#endregion

namespace Cornerstone.Parsers.CSharp;

/// <summary>
/// Represents the data for a CSharp token.
/// </summary>
public class CSharpTokenData : TokenData<CSharpTokenData, SyntaxKind>
{
	#region Methods

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			CSharpTokenData value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	/// <inheritdoc />
	public override void WriteTo(CodeSyntaxHtmlWriter writer)
	{
		switch (Type)
		{
			case SyntaxKind.RegionDirectiveTrivia:
			{
				writer.WriteSpan(SubStringUsingAbsoluteIndexes(TokenIndexes[0], TokenIndexes[2], true), SyntaxColor.KeywordMuted);
				writer.WriteRaw(SubStringUsingAbsoluteIndexes(TokenIndexes[3], EndIndex - 1, true));
				return;
			}
			case SyntaxKind.EndRegionDirectiveTrivia:
			{
				writer.WriteSpan(ToString(), SyntaxColor.KeywordMuted);
				return;
			}
			case SyntaxKind.AttributeList:
			{
				writer.WriteRaw(SubStringUsingAbsoluteIndexes(StartIndex, TokenIndexes[1] - 1, true));
				writer.WriteSpan(SubStringUsingAbsoluteIndexes(TokenIndexes[1], TokenIndexes[2] - 1, true), SyntaxColor.Identifier);
				writer.WriteRaw(SubStringUsingAbsoluteIndexes(TokenIndexes[2], EndIndex - 1, true));
				return;
			}
			case SyntaxKind.IdentifierToken:
			{
				writer.WriteSpan(ToString(), SyntaxColor.Identifier);
				return;
			}
			case SyntaxKind.StringLiteralToken:
			case SyntaxKind.MultiLineRawStringLiteralToken:
			case SyntaxKind.Utf8StringLiteralToken:
			{
				writer.WriteSpan(ToString(), SyntaxColor.String);
				return;
			}
			case SyntaxKind.Parameter:
			{
				writer.WriteSpan(ToString(), SyntaxColor.Method);
				return;
			}
			case SyntaxKind.ReturnKeyword:
			{
				writer.WriteSpan(ToString(), SyntaxColor.KeywordControl);
				return;
			}
			case SyntaxKind.UsingDirective:
			{
				writer.WriteSpan(SubStringUsingAbsoluteIndexes(TokenIndexes[0], TokenIndexes[1], true), SyntaxColor.Keyword);
				writer.WriteRaw(SubStringUsingAbsoluteIndexes(TokenIndexes[2], TokenIndexes[3], true));
				return;
			}
			case SyntaxKind.NamespaceKeyword:
			case SyntaxKind.StaticKeyword:
			case SyntaxKind.UsingKeyword:
			case SyntaxKind.PublicKeyword:
			case SyntaxKind.PrivateKeyword:
			case SyntaxKind.ProtectedKeyword:
			case SyntaxKind.InternalKeyword:
			case SyntaxKind.ClassKeyword:
			case SyntaxKind.IntKeyword:
			case SyntaxKind.UIntKeyword:
			case SyntaxKind.LongKeyword:
			case SyntaxKind.ULongKeyword:
			{
				writer.WriteSpan(ToString(), SyntaxColor.Keyword);
				return;
			}
			default:
			{
				writer.WriteRaw(ToString());
				return;
			}
		}
	}

	public override void WriteTo(HtmlWriter writer)
	{
		switch (Type)
		{
			default:
			{
				var elementValue = ToString();
				writer.WriteElement("p", elementValue);
				return;
			}
		}
	}

	#endregion
}