#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Text;
using Cornerstone.Text.CodeGenerators;

#endregion

namespace Cornerstone.Parsers;

public abstract class TextProcessor<T>
	: TextProcessor where T : class
{
	#region Fields

	public readonly IQueue<T> Pool;

	#endregion

	#region Constructors

	protected TextProcessor(IStringBuffer buffer, IQueue<T> pool) : base(buffer)
	{
		Pool = pool;
	}

	#endregion

	#region Methods

	public abstract T CreateOrUpdateSection(int type, int startOffset, int endOffset, params int[] offsets);

	public T NextSection()
	{
		if (Position >= Buffer.Count)
		{
			return null!;
		}

		if (TryProcessContinuation(out var section))
		{
			return section;
		}

		var start = Position;
		if (Newlines.Contains(Buffer[Position]))
		{
			Position = ConsumeNewLines();
			CurrentState = LexerStateDefault;
			return CreateOrUpdateSection(TokenTypeNewLine, start, Position);
		}
		if (Whitespace.Contains(Buffer[Position]))
		{
			Position = ConsumeWhitespace();
			CurrentState = LexerStateDefault;
			return CreateOrUpdateSection(TokenTypeWhitespace, start, Position);
		}

		if (TryProcessPosition(out section))
		{
			return section;
		}

		return ReadText();
	}

	public IEnumerable<T> Process()
	{
		StartProcessing();

		while (NextSection() is { } section)
		{
			yield return section;
		}
	}

	public virtual bool TryProcessContinuation(out T token)
	{
		token = null!;
		return false;
	}

	public virtual bool TryProcessPosition(out T token)
	{
		token = null!;
		return false;
	}

	protected T ReadLineEndings()
	{
		// Track start and always move at least 1 position.
		var start = Position;
		Position++;
		ConsumeNewLines();
		return CreateOrUpdateSection(TokenTypeNewLine, start, Position);
	}

	/// <summary>
	/// This will already read some amount of text. Minimal length will be 1 character.
	/// </summary>
	/// <returns> The token representing the text. </returns>
	protected virtual T ReadText()
	{
		// Track start and always move at least 1 position.
		var start = Position++;
		var wasEndOfLine = AtEndOfLine || AtIndentation;
		var isOnlyEndOfLines = true;

		while (Position < Buffer.Count)
		{
			if (IsStartCharacter())
			{
				break;
			}

			var current = Buffer[Position];

			AtEndOfLine = Newlines.Contains(current);
			AtWhitespace = Whitespace.Contains(current);

			// AtIndentation = true only if we are still in leading whitespace since last newline
			var isIndentChar = Indentation.Contains(current);

			if (AtEndOfLine)
			{
				// We just hit a newline → next character will be at start of new line
				AtIndentation = true;
				wasEndOfLine = true;
			}
			else if (isIndentChar && wasEndOfLine)
			{
				// Still in indentation phase after a newline (or document start)
				AtIndentation = true;
			}
			else
			{
				// We hit real content → indentation phase is over for this line
				AtIndentation = false;
				wasEndOfLine = false;
			}

			isOnlyEndOfLines &= AtEndOfLine;

			Position++;
		}

		return CreateOrUpdateSection(
			isOnlyEndOfLines
				? TokenTypeNewLine
				: TokenTypeText,
			start, Position
		);
	}

	#endregion
}

public abstract class TextProcessor : TextService
{
	#region Fields

	public static readonly int LexerStateDefault;

	public static readonly Dictionary<int, string> RegisteredTokenStatesCodeNames;
	public static readonly Dictionary<int, SyntaxColor> RegisteredTokenTypeColors;
	public static readonly Dictionary<int, string> RegisteredTokenTypesCodeNames;
	public static readonly Dictionary<int, string> RegisteredTokenTypesDisplayName;

	public static readonly int TokenTypeError;
	public static readonly int TokenTypeNewLine;
	public static readonly int TokenTypeText;
	public static readonly int TokenTypeWhitespace;

	#endregion

	#region Constructors

	protected TextProcessor(IStringBuffer buffer) : base(buffer)
	{
	}

	static TextProcessor()
	{
		RegisteredTokenStatesCodeNames = new();
		RegisteredTokenTypeColors = new();
		RegisteredTokenTypesCodeNames = new();
		RegisteredTokenTypesDisplayName = new();

		LexerStateDefault = RegisterTokenState(nameof(TextProcessor), nameof(LexerStateDefault), 0);

		TokenTypeText = RegisterTokenType("Text", nameof(TextProcessor), nameof(TokenTypeText), 0, SyntaxColor.None);
		TokenTypeError = RegisterTokenType("Error", nameof(TextProcessor), nameof(TokenTypeError), 1, SyntaxColor.Error);
		TokenTypeNewLine = RegisterTokenType("NewLine", nameof(TextProcessor), nameof(TokenTypeNewLine), 2, SyntaxColor.None);
		TokenTypeWhitespace = RegisterTokenType("Whitespace", nameof(TextProcessor), nameof(TokenTypeWhitespace), 3, SyntaxColor.None);

		CodeBuilder.RegisterPropertyValueProvider(TryGetTokenizerStateOrTypeCode);
	}

	#endregion

	#region Properties

	public bool AtEndOfLine { get; set; }

	public bool AtIndentation { get; set; }

	public bool AtWhitespace { get; set; }

	public int CurrentState { get; protected set; }

	#endregion

	#region Methods

	public static string GetTokenizerTypeName(int value)
	{
		if (RegisteredTokenTypesDisplayName.TryGetValue(value, out var name))
		{
			return name;
		}

		return value.ToString();
	}

	public abstract bool IsStartCharacter();

	public virtual void StartProcessing()
	{
		Position = 0;
		AtEndOfLine = true;
		AtIndentation = true;
		AtWhitespace = true;
	}

	public static string TryGetTokenizerStateOrTypeCode(SourceTypeInfo typeInfo, string name, object value)
	{
		if (typeInfo.Type != typeof(Token))
		{
			return null;
		}

		return TryGetTokenizerStateOrTypeCode(name, value);
	}

	public static string TryGetTokenizerStateOrTypeCode(string name, object value)
	{
		if (value is not int intValue)
		{
			return null;
		}

		var propName = name ?? string.Empty;

		if ((propName.Equals("Type", StringComparison.OrdinalIgnoreCase)
				|| propName.Contains("Type", StringComparison.OrdinalIgnoreCase))
			&& GetTokenTypeCodeName(propName, intValue, out var code))
		{
			return code;
		}

		if (propName.Equals("State", StringComparison.OrdinalIgnoreCase)
			|| propName.Contains("State", StringComparison.OrdinalIgnoreCase))
		{
			if (RegisteredTokenStatesCodeNames.TryGetValue(intValue, out code))
			{
				return code;
			}
		}

		return null;
	}

	protected static int RegisterTokenState(string tokenizerName, string memberName, int value)
	{
		var qualifiedName = $"{tokenizerName}.{memberName}";

		if (RegisteredTokenStatesCodeNames.TryGetValue(value, out var existing))
		{
			throw new InvalidOperationException($"Token state value collision: {value} already used by '{existing}'.");
		}

		RegisteredTokenStatesCodeNames[value] = qualifiedName;
		return value;
	}

	protected static int RegisterTokenType(string displayName, string tokenizerName, string memberName, int value, SyntaxColor syntaxColor)
	{
		var qualifiedName = $"{tokenizerName}.{memberName}";

		if (RegisteredTokenTypesCodeNames.TryGetValue(value, out var existing))
		{
			throw new InvalidOperationException(
				$"Token type value collision detected! Value {value} is already registered as '{existing}'. " +
				$"Attempted to register '{qualifiedName}' from tokenizer '{tokenizerName}'. " +
				"All token type values across all tokenizers must be unique.");
		}

		RegisteredTokenTypesDisplayName[value] = displayName;
		RegisteredTokenTypesCodeNames[value] = qualifiedName;
		RegisteredTokenTypeColors[value] = syntaxColor;

		return value;
	}

	internal static bool GetTokenTypeCodeName(string propertyName, int intValue, out string code)
	{
		if (RegisteredTokenTypesCodeNames.TryGetValue(intValue, out var name))
		{
			code = name;
			return true;
		}

		code = null;
		return false;
	}

	internal static bool GetTokenTypeDisplayName(string propertyName, int intValue, out string code)
	{
		if (RegisteredTokenTypesDisplayName.TryGetValue(intValue, out var name))
		{
			code = name;
			return true;
		}

		code = null;
		return false;
	}

	#endregion
}