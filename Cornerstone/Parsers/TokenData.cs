#region References

using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Parsers.Html;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers;

/// <summary>
/// Represents data for a token.
/// </summary>
/// <typeparam name="TTokenData"> The type of the token data object. </typeparam>
/// <typeparam name="TType"> The type of the token type. </typeparam>
public abstract class TokenData<TTokenData, TType> : Notifiable<TTokenData>
	where TTokenData : TokenData<TTokenData, TType>, new()
{
	#region Fields

	private TextDocument _document;

	#endregion

	#region Properties

	/// <summary>
	/// Represents the character position.
	/// </summary>
	public int ColumnNumber { get; set; }

	public int EndIndex { get; set; }

	/// <summary>
	/// Gets the character at the specified index. Returns <see cref="TextDocument.NullChar" /> if <paramref name="index" />
	/// is not valid.
	/// </summary>
	/// <param name="index"> 0-based position of the character to return. </param>
	public char this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (index >= 0) && (index < Length) ? _document[StartIndex + index] : TextDocument.NullChar;
	}

	public int Length => EndIndex - StartIndex;

	/// <summary>
	/// Represents the line number.
	/// </summary>
	public int LineNumber { get; set; }

	public int StartIndex { get; set; }

	public int[] TokenIndexes { get; set; }

	/// <summary>
	/// The type of the token.
	/// </summary>
	public TType Type { get; set; }

	/// <summary>
	/// Cached value for the token type.
	/// </summary>
	public object Value { get; set; }

	#endregion

	#region Methods

	public string SubString(int index, int length)
	{
		return _document?.SubString(index, length) ?? string.Empty;
	}

	public string SubStringUsingAbsoluteIndexes(int startIndex, int endIndex, bool inclusive)
	{
		return _document?.SubStringUsingAbsoluteIndexes(startIndex, endIndex, inclusive) ?? string.Empty;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return _document?.SubString(StartIndex, Length) ?? string.Empty;
	}

	/// <summary>
	/// Update the TokenData with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(TTokenData update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			ColumnNumber = update.ColumnNumber;
			EndIndex = update.EndIndex;
			LineNumber = update.LineNumber;
			StartIndex = update.StartIndex;
			TokenIndexes = update.TokenIndexes?.ToArray();
			Type = update.Type;
			Value = update.Value;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(ColumnNumber)), x => x.ColumnNumber = update.ColumnNumber);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(EndIndex)), x => x.EndIndex = update.EndIndex);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(LineNumber)), x => x.LineNumber = update.LineNumber);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(StartIndex)), x => x.StartIndex = update.StartIndex);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(TokenIndexes)), x => x.TokenIndexes = update.TokenIndexes?.ToArray());
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Type)), x => x.Type = update.Type);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Value)), x => x.Value = update.Value);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			TTokenData value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	/// <summary>
	/// Write the token to the code syntax html writer.
	/// </summary>
	/// <param name="writer"> The writer to use. </param>
	public virtual void WriteTo(CodeSyntaxHtmlWriter writer)
	{
		writer.WriteRaw(ToString());
	}

	/// <summary>
	/// Write the token to the code syntax html writer.
	/// </summary>
	/// <param name="writer"> The writer to use. </param>
	public virtual void WriteTo(HtmlWriter writer)
	{
		writer.WriteRaw(ToString());
	}

	internal void SetDocument(TextDocument document)
	{
		_document = document;
	}

	#endregion
}