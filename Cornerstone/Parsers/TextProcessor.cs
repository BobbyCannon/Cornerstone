#region References

using System;
using System.Collections.Generic;
using System.Drawing;
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

	/// <summary>
	/// Extra sections produced by a single match (e.g. expanded emphasis interiors).
	/// Drained by <see cref="NextSection" /> before reading the buffer again.
	/// </summary>
	private Queue<T> _pendingSections;

	#endregion

	#region Constructors

	protected TextProcessor(IStringBuffer buffer, IQueue<T> pool) : base(buffer)
	{
		Pool = pool;
	}

	#endregion

	#region Methods

	public abstract T CreateOrUpdateSection(int type, int startOffset, int endOffset, uint? foreground = null, uint? background = null,
		bool? bold = null, bool? italic = null, bool? strikethrough = null, params int[] offsets);

	/// <summary>
	/// Queues additional sections to be returned by subsequent <see cref="NextSection" /> calls.
	/// </summary>
	protected void EnqueuePending(T section)
	{
		if (section is null)
		{
			return;
		}

		_pendingSections ??= new Queue<T>(8);
		_pendingSections.Enqueue(section);
	}

	public T NextSection()
	{
		if ((_pendingSections is { Count: > 0 }))
		{
			return _pendingSections.Dequeue();
		}

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

	public override void StartProcessing()
	{
		base.StartProcessing();
		_pendingSections?.Clear();
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
	/// <returns> The section representing the text. </returns>
	protected virtual T ReadText()
	{
		// Track start and always move at least 1 position.
		// The first character must update EOL/indent state — skipping it left
		// wasEndOfLine true so a following space kept AtIndentation, and mid-line
		// '+' / '-' / '*' were misread as list markers (e.g. **2 + 2 = 4**).
		var start = Position;
		var wasEndOfLine = AtEndOfLine || AtIndentation;
		var isOnlyEndOfLines = true;

		void ConsumeOne(char current)
		{
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

		ConsumeOne(Buffer[Position]);

		while (Position < Buffer.Count)
		{
			if (IsStartCharacter())
			{
				break;
			}

			ConsumeOne(Buffer[Position]);
		}

		return CreateOrUpdateSection(
			isOnlyEndOfLines
				? TokenTypeNewLine
				: TokenTypeText,
			start, Position
		);
	}

	protected virtual bool TryProcessContinuation(out T section)
	{
		section = null!;
		return false;
	}

	/// <summary>
	/// Helper method to detect and consume a delimited section: startPattern + content + endPattern.
	/// Supports multi-character start/end patterns and optional state management.
	/// </summary>
	/// <param name="delimiter"> The start/end delimiter (e.g. \", *, `) </param>
	/// <param name="sectionType"> The section type to assign to the entire delimited section </param>
	/// <param name="selection"> The section if it matched and was processed. </param>
	/// <returns> True if a delimited section was successfully processed. </returns>
	protected bool TryProcessDelimitedInlineSelection(char delimiter, int sectionType, out T selection)
	{
		selection = null!;
		var start = Position;

		// Count consecutive opening delimiters
		var n = 0;
		while (((Position + n) < Buffer.Count) && (Buffer[Position + n] == delimiter))
		{
			n++;
		}

		if (n == 0)
		{
			return false;
		}

		var contentStart = Position + n;
		var position = contentStart;

		// Inline spans cannot contain newlines
		while (position < Buffer.Count)
		{
			if (char.IsControl(Buffer[position]))
			{
				return false;
			}

			if (Buffer[position] == delimiter)
			{
				// Check if we have n consecutive delimiters here
				var endCount = 0;
				var tempPos = position;
				while ((tempPos < Buffer.Count) && (Buffer[tempPos] == delimiter) && (endCount < n))
				{
					endCount++;
					tempPos++;
				}

				if (endCount == n)
				{
					// Found potential closing delimiter
					// Check if content contains n or more consecutive delimiters
					var maxDelimiterInContent = 0;
					var currentRun = 0;
					for (var i = contentStart; i < position; i++)
					{
						if (Buffer[i] == delimiter)
						{
							currentRun++;
							if (currentRun > maxDelimiterInContent)
							{
								maxDelimiterInContent = currentRun;
							}
						}
						else
						{
							currentRun = 0;
						}
					}

					if (maxDelimiterInContent >= n)
					{
						// Invalid delimiter length for this content according to CommonMark
						return false;
					}

					// Valid delimited selection. Offsets = content region (excludes delimiters)
					// so MarkdownInlineProjector can strip markers for TokenTypeInlineCode, etc.
					var end = position + n;
					Position = end;
					selection = CreateOrUpdateSection(sectionType, start, end, offsets: [contentStart, position]);
					return true;
				}
			}
			position++;
		}

		return false;
	}

	/// <summary>
	/// Helper method to detect and consume a delimited section: startPattern + content + endPattern.
	/// Supports multi-character start/end patterns and optional state management.
	/// </summary>
	/// <param name="startPattern"> The starting delimiter (e.g. \", *, ```) </param>
	/// <param name="endPattern"> The ending delimiter. </param>
	/// <param name="sectionType"> The section type to assign to the entire delimited section </param>
	/// <param name="block"> The section if it matched and was processed. </param>
	/// <returns> True if a delimited section was successfully processed. </returns>
	protected virtual bool TryProcessDelimitedSection(
		string startPattern,
		string endPattern,
		int sectionType,
		out T block,
		bool requiresLeadingNewLine = false,
		bool allowLeadingIndentation = false)
	{
		if (!TryMatch(Position, startPattern))
		{
			block = null;
			return false;
		}

		var start = Position;
		var position = start + startPattern.Length;
		var offset1 = position;

		while (position < Buffer.Count)
		{
			// Check for end pattern
			if (TryMatch(position, endPattern))
			{
				var offest2 = position;
				position += endPattern.Length;
				Position = position;

				// Create section for the entire delimited section (including start + content + end)
				block = CreateOrUpdateSection(sectionType, start, position, offsets: [offset1, offest2]);
				return true;
			}

			position++;
		}

		// If we reach here, we hit EOF or newline without finding the end pattern
		block = null;
		return false;
	}

	/// <summary>
	/// Helper method to detect and consume a delimited section: startPattern + content + endPattern.
	/// Supports multi-character start/end patterns and optional state management.
	/// </summary>
	/// <param name="startPattern"> The starting delimiter (e.g. \", *, ```) </param>
	/// <param name="endPattern"> The ending delimiter. </param>
	/// <param name="sectionType"> The type to assign to the entire delimited section </param>
	/// <param name="section"> The section if it matched and was processed. </param>
	/// <param name="except"> An optional set of characters that are not accepted. </param>
	/// <returns> True if a delimited section was successfully processed. </returns>
	protected bool TryProcessDelimitedSection(string startPattern, string endPattern, int sectionType, out T section, params char[] except)
	{
		return TryProcessDelimitedSection(startPattern, endPattern, sectionType, out section, null, except);
	}

	/// <summary>
	/// Helper method to detect and consume a **nested** delimited section using single characters.
	/// Fully supports nesting (e.g. [outer [inner] more] ).
	/// Returns the entire section from the opening startChar to the matching closing endChar.
	/// </summary>
	/// <param name="startChar"> The starting delimiter (e.g. '[') </param>
	/// <param name="endChar"> The ending delimiter (e.g. ']') </param>
	/// <param name="sectionType"> The section type for the entire nested section </param>
	/// <param name="section"> The resulting section (null if not matched or unclosed) </param>
	/// <returns> True if a complete nested delimited section was processed. </returns>
	protected bool TryProcessDelimitedSection(char startChar, char endChar, int sectionType, out T section)
	{
		if ((Position >= Buffer.Count) || (Buffer[Position] != startChar))
		{
			section = null!;
			return false;
		}

		var start = Position;
		var position = start + 1; // skip the opening startChar
		var nestingLevel = 1; // we already saw one opening

		while (position < Buffer.Count)
		{
			var c = Buffer[position];

			if (c == startChar)
			{
				nestingLevel++;
			}
			else if (c == endChar)
			{
				nestingLevel--;
				if (nestingLevel == 0)
				{
					// Found the matching closing delimiter
					position++; // include the closing char
					Position = position;
					CurrentState = LexerStateDefault;
					section = CreateOrUpdateSection(sectionType, start, position);
					return true;
				}
			}

			position++;
		}

		// Unclosed - we reached EOF without finding matching end
		// You can decide the policy: either treat as error, or as text.
		// Here we fail (return false) so the caller can fall back to plain text.
		CurrentState = LexerStateDefault;
		section = null!;
		return false;
	}

	/// <summary>
	/// Helper method to detect and consume a delimited section: startPattern + content + endPattern.
	/// Supports multi-character start/end patterns, optional state management, and extra offsets.
	/// </summary>
	/// <param name="startPattern"> The starting delimiter (e.g. \", *, ```) </param>
	/// <param name="endPattern"> The ending delimiter. </param>
	/// <param name="sectionType"> The section type to assign to the entire delimited section </param>
	/// <param name="section"> The section if it matched and was processed. </param>
	/// <param name="extraOffsets"> Additional offsets to pass to CreateOrUpdateSection. </param>
	/// <param name="except"> An optional set of characters that are not accepted. </param>
	/// <returns> True if a delimited section was successfully processed. </returns>
	protected bool TryProcessDelimitedSection(string startPattern, string endPattern, int sectionType, out T section, int[] extraOffsets, params char[] except)
	{
		if (!TryMatch(Position, startPattern))
		{
			section = null!;
			return false;
		}

		var start = Position;
		var position = start + startPattern.Length;

		while (position < Buffer.Count)
		{
			if (except.Length > 0)
			{
				foreach (var c in except)
				{
					if (Buffer[position] != c)
					{
						continue;
					}

					section = null!;
					return false;
				}
			}

			// Check for end pattern
			if (TryMatch(position, endPattern))
			{
				position += endPattern.Length;
				CurrentState = LexerStateDefault;
				Position = position;

				section = CreateOrUpdateSection(sectionType, start, position, offsets: extraOffsets ?? []);
				return true;
			}

			position++;
		}

		// If we reach here, we hit EOF or newline without finding the end pattern
		CurrentState = LexerStateDefault;
		section = null!;
		return false;
	}

	/// <summary>
	/// Helper method to detect and consume a **nested** delimited token using single characters.
	/// Fully supports nesting (e.g. [outer [inner] more] ).
	/// Returns the entire section from the opening startChar to the matching closing endChar.
	/// </summary>
	/// <param name="startChar"> The starting delimiter (e.g. '[') </param>
	/// <param name="endChar"> The ending delimiter (e.g. ']') </param>
	/// <param name="tokenType"> The token type for the entire nested section </param>
	/// <param name="token"> The resulting token (null if not matched or unclosed) </param>
	/// <returns> True if a complete nested delimited token was processed. </returns>
	protected bool TryProcessDelimitedToken(char startChar, char endChar, int tokenType, out T token)
	{
		if ((Position >= Buffer.Count) || (Buffer[Position] != startChar))
		{
			token = null;
			return false;
		}

		var start = Position;
		var position = start + 1; // skip the opening startChar
		var nestingLevel = 1; // we already saw one opening

		while (position < Buffer.Count)
		{
			var c = Buffer[position];

			if (c == startChar)
			{
				nestingLevel++;
			}
			else if (c == endChar)
			{
				nestingLevel--;
				if (nestingLevel == 0)
				{
					// Found the matching closing delimiter
					position++; // include the closing char
					Position = position;
					CurrentState = LexerStateDefault;
					token = CreateOrUpdateSection(tokenType, start, position);
					return true;
				}
			}

			position++;
		}

		// Unclosed - we reached EOF without finding matching end
		// You can decide the policy: either treat as error, or as text.
		// Here we fail (return false) so the caller can fall back to plain text.
		CurrentState = LexerStateDefault;
		token = null;
		return false;
	}

	protected virtual bool TryProcessPosition(out T section)
	{
		section = null!;
		return false;
	}

	#endregion
}

public abstract class TextProcessor : TextReader
{
	#region Fields

	public static readonly int LexerStateDefault;

	public static readonly Dictionary<int, string> RegisteredTokenStatesCodeNames;
	public static readonly Dictionary<int, SyntaxKind> RegisteredTokenTypeColors;
	public static readonly Dictionary<int, string> RegisteredTokenTypesCodeNames;
	public static readonly Dictionary<int, string> RegisteredTokenTypesDisplayName;

	public static readonly int TokenTypeError;
	public static readonly int TokenTypeNewLine;
	public static readonly int TokenTypeText;
	public static readonly int TokenTypeUnknown;
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

		TokenTypeUnknown = RegisterTokenType("Unknown", nameof(TextProcessor), nameof(TokenTypeUnknown), 0, SyntaxKind.None);
		TokenTypeText = RegisterTokenType("Text", nameof(TextProcessor), nameof(TokenTypeText), 1, SyntaxKind.None);
		TokenTypeError = RegisterTokenType("Error", nameof(TextProcessor), nameof(TokenTypeError), 2, SyntaxKind.Error);
		TokenTypeNewLine = RegisterTokenType("NewLine", nameof(TextProcessor), nameof(TokenTypeNewLine), 3, SyntaxKind.None);
		TokenTypeWhitespace = RegisterTokenType("Whitespace", nameof(TextProcessor), nameof(TokenTypeWhitespace), 4, SyntaxKind.None);

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

	protected static int RegisterTokenType(string displayName, string tokenizerName, string memberName, int value, SyntaxKind syntaxColor)
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

	/// <summary>
	/// Helper method to detect a delimited token (startPattern + content + endPattern).
	/// Does NOT consume the token — only calculates the end offset.
	/// </summary>
	protected bool TryProcessDelimitedToken(
		string startPattern,
		string endPattern,
		int tokenType,
		out int endOffset)
	{
		endOffset = Position;

		if (!TryMatch(Position, startPattern))
		{
			return false;
		}

		var position = Position + startPattern.Length;
		var count = Buffer.Count;

		while (position < count)
		{
			if (TryMatch(position, endPattern))
			{
				position += endPattern.Length;
				endOffset = position;
				return true;
			}

			position++;
		}

		return false;
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