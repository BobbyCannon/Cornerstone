#region References

using System;
using System.Globalization;
using System.Text;

#endregion

namespace Cornerstone.VisualStudio.Core.Parsing;

internal enum SelectorStatement
{
	Start,
	Middle,
	Colon,
	Class,
	Name,
	CanHaveType,
	Traversal,
	TypeName,
	Property,
	AttachedProperty,
	Template,
	Value,
	Function,
	FunctionArgs,
	End
}

internal ref struct SelectorParser
{
	#region Fields

	private ParserContext _context;

	#endregion

	#region Constructors

	private SelectorParser(ReadOnlySpan<char> data)
	{
		_context = new ParserContext(data);
	}

	#endregion

	#region Properties

	public string? Class => _context.GetRange(_context.ClassNameStart, _context.ClassNameEnd).ToString();

	public string? ElementName => _context.GetRange(_context.NameStart, _context.NameEnd).ToString();

	public string? FunctionName => _context.GetRange(_context.FunctionNameStart, _context.FunctionNameEnd).ToString();

	public bool IsError => _context.IsError;

	public bool IsTemplate => _context.IsTemplate;

	public int? LastParsedPosition => _context.LastParsedPosition;

	public int LastSegmentStartPosition => _context.LastSegmentStartPosition;

	public string? Namespace => _context.GetRange(_context.NamespaceStart, _context.NamespaceEnd).ToString();

	public SelectorStatement PreviousStatement => _context.PreviousStatement;

	public string? PropertyName => _context.GetRange(_context.PropertyNameStart, _context.PropertyNameEnd).ToString();

	public SelectorStatement Statement => _context.Statement;

	public string? TemplateOwner
	{
		get
		{
			var sb = new StringBuilder();
			if (_context.NamespaceTemplateOwnerEnd > -1)
			{
				#if NET5_0_OR_GREATER
				sb.Append(_context.GetRange(_context.NamespaceTemplateOwnerStart, _context.NamespaceTemplateOwnerEnd));
				#else
				sb.Append(_context.GetRange(_context.NamespaceTemplateOwnerStart, _context.NamespaceTemplateOwnerEnd).ToArray());
				#endif
				sb.Append(':');
			}
			#if NET5_0_OR_GREATER
			sb.Append(_context.GetRange(_context.TemplateOwnerStart, _context.TemplateOwnerEnd));
			#else
			sb.Append(_context.GetRange(_context.TemplateOwnerStart, _context.TemplateOwnerEnd).ToArray());
			#endif
			return sb.ToString();
		}
	}

	public string? TypeName => _context.GetRange(_context.TypeNameStart, _context.TypeNameEnd).ToString();

	public string? Value => _context.GetRange(_context.ValueStart, _context.ValueEnd).ToString();

	#endregion

	#region Methods

	public static SelectorParser Parse(ReadOnlySpan<char> data)
	{
		var selector = new SelectorParser(data);
		selector.Parse();
		return selector;
	}

	private static bool Expect(ref ParserContext r, char c)
	{
		if (r.End || !r.TakeIf(c))
		{
			r.IsError = true;
			return false;
		}
		return true;
	}

	private void Parse()
	{
		Parse(ref _context);
	}

	private static void Parse(ref ParserContext context, char? end = null)
	{
		while (!context.End && !context.IsError && (context.Statement != SelectorStatement.End))
		{
			switch (context.Statement)
			{
				case SelectorStatement.Start:
					ParseStart(ref context);
					break;
				case SelectorStatement.Middle:
					ParseMiddle(ref context, end);
					break;
				case SelectorStatement.Colon:
					ParseColon(ref context);
					break;
				case SelectorStatement.Class:
					ParseClass(ref context);
					break;
				case SelectorStatement.Name:
					ParseName(ref context);
					break;
				case SelectorStatement.CanHaveType:
					ParseCanHaveType(ref context);
					break;
				case SelectorStatement.Traversal:
					ParseTraversal(ref context);
					break;
				case SelectorStatement.TypeName:
					ParseTypeName(ref context);
					break;
				case SelectorStatement.Property:
					ParseProperty(ref context);
					break;
				case SelectorStatement.AttachedProperty:
					ParseAttachedProperty(ref context);
					break;
				case SelectorStatement.Template:
					ParseTemplate(ref context);
					break;
				case SelectorStatement.FunctionArgs:
					ParseFunctionArgs(ref context);
					break;
				case SelectorStatement.End:
					break;
			}
		}
	}

	private static void ParseAttachedProperty(ref ParserContext r)
	{
		r.LastParsedPosition = r.Position;
		ParseType(ref r);
		if (r.IsError)
		{
			return;
		}
		r.LastParsedPosition = r.Position;
		if (r.End || !r.TakeIf('.'))
		{
			r.IsError = true;
			return;
		}
		r.PropertyNameStart = r.Position;
		if (r.End)
		{
			r.IsError = true;
			return;
		}
		var property = r.ParseIdentifier();
		if (r.End || property.IsEmpty)
		{
			r.IsError = true;
			return;
		}
		r.PropertyNameEnd = r.Position;

		if (!r.TakeIf(')'))
		{
			r.IsError = true;
			return;
		}
		r.SkipWhitespace();
		r.LastParsedPosition = r.Position;

		if (r.End || !r.TakeIf('='))
		{
			r.IsError = true;
			return;
		}
		r.ValueStart = r.Position;
		r.Statement = SelectorStatement.Value;
		_ = r.TakeUntil(']');
		r.ValueEnd = r.Position;
		if (Expect(ref r, ']'))
		{
			r.IsError = true;
			return;
		}
		r.LastParsedPosition = r.Position;
		if (!r.End)
		{
			r.Statement = SelectorStatement.Middle;
		}
	}

	private static void ParseCanHaveType(ref ParserContext r)
	{
		if (r.TakeIf('['))
		{
			r.LastParsedPosition = r.Position;
			r.Statement = SelectorStatement.Property;
		}
		else
		{
			r.Statement = SelectorStatement.Middle;
		}
	}

	private static void ParseClass(ref ParserContext r)
	{
		r.ClassNameStart = r.Position;
		var @class = r.ParseStyleClass();
		if (@class.IsEmpty)
		{
			r.IsError = true;
			return;
		}
		r.ClassNameEnd = r.Position;
		r.LastParsedPosition = r.Position;
		r.Statement = SelectorStatement.CanHaveType;
	}

	private static void ParseColon(ref ParserContext r)
	{
		var start = r.Position;
		var identifier = r.ParseStyleClass();

		if (identifier.IsEmpty)
		{
			r.IsError = true;
			return;
		}

		const string isKeyword = "is";
		const string notKeyword = "not";
		const string nthChildKeyword = "nth-child";
		const string nthLastChildKeyword = "nth-last-child";

		if (identifier.SequenceEqual(isKeyword.AsSpan()))
		{
			r.FunctionNameStart = start;
			r.Statement = SelectorStatement.Function;
			r.LastParsedPosition = r.Position;
			if (r.TakeIf('('))
			{
				r.Statement = SelectorStatement.FunctionArgs;
				r.FunctionNameEnd = r.Position - 1;
				if (r.End)
				{
					return;
				}
				r.Statement = SelectorStatement.TypeName;
				ParseType(ref r);
				if (!Expect(ref r, ')'))
				{
					return;
				}
				r.Statement = SelectorStatement.Middle;
			}
		}
		else if (identifier.SequenceEqual(notKeyword.AsSpan()))
		{
			r.FunctionNameStart = start;
			r.Statement = SelectorStatement.Function;
			r.LastParsedPosition = r.Position;
			if (r.TakeIf('('))
			{
				r.FunctionNameEnd = r.Position - 1;
				r.Statement = SelectorStatement.FunctionArgs;
				Parse(ref r, ')');
				if (r.IsError)
				{
					return;
				}
				r.Statement = SelectorStatement.FunctionArgs;
				Expect(ref r, ')');
				if (r.IsError)
				{
					return;
				}
				r.Statement = SelectorStatement.Middle;
			}
		}
		else if (identifier.SequenceEqual(nthChildKeyword.AsSpan()))
		{
			r.FunctionNameStart = start;
			r.Statement = SelectorStatement.Function;
			r.LastParsedPosition = r.Position;
			if (r.TakeIf('('))
			{
				r.FunctionNameEnd = r.Position - 1;
				r.Statement = SelectorStatement.FunctionArgs;
				r.TakeUntil(')');
				Expect(ref r, ')');
				if (r.IsError)
				{
					return;
				}
				r.Statement = SelectorStatement.Middle;
				r.LastParsedPosition = r.Position;
			}
		}
		else if (identifier.SequenceEqual(nthLastChildKeyword.AsSpan()))
		{
			r.FunctionNameStart = start;
			r.Statement = SelectorStatement.Function;
			r.LastParsedPosition = r.Position;
			if (r.TakeIf('('))
			{
				r.FunctionNameEnd = r.Position - 1;
				r.Statement = SelectorStatement.FunctionArgs;
				r.TakeUntil(')');
				Expect(ref r, ')');
				if (r.IsError)
				{
					return;
				}
				r.LastParsedPosition = r.Position;
				r.Statement = SelectorStatement.Middle;
			}
		}
		else
		{
			r.ClassNameStart = start;
			r.ClassNameEnd = r.Position;
			r.LastParsedPosition = r.Position;
			r.Statement = SelectorStatement.CanHaveType;
		}
	}

	private static void ParseFunctionArgs(ref ParserContext context)
	{
		context.Statement = SelectorStatement.Middle;
	}

	private static void ParseMiddle(ref ParserContext context, char? end)
	{
		if (context.TakeIf(':'))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.Colon;
		}
		else if (context.TakeIf('.'))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.Class;
		}
		else if (context.TakeIf(char.IsWhiteSpace) || (context.Peek == '>'))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.Traversal;
		}
		else if (context.TakeIf('/'))
		{
			context.Statement = SelectorStatement.Template;
		}
		else if (context.TakeIf('#'))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.Name;
		}
		else if (context.TakeIf(','))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.Start;
		}
		else if (context.TakeIf('^'))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.CanHaveType;
		}
		else if (end.HasValue && !context.End && (context.Peek == end.Value))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.End;
		}
		else
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.TypeName;
		}
	}

	private static void ParseName(ref ParserContext r)
	{
		r.NameStart = r.Position;
		var name = r.ParseIdentifier();
		if (name.IsEmpty)
		{
			r.IsError = true;
			return;
		}
		r.NameEnd = r.Position;
		if (!r.End)
		{
			r.Statement = SelectorStatement.CanHaveType;
		}
	}

	private static void ParseProperty(ref ParserContext r)
	{
		r.LastParsedPosition = r.Position;
		r.PropertyNameStart = r.Position;
		var property = r.ParseIdentifier();

		if (r.End)
		{
			r.IsError = true;
			return;
		}

		if (r.TakeIf('('))
		{
			r.Statement = SelectorStatement.AttachedProperty;
			return;
		}
		if (!r.TakeIf('='))
		{
			r.IsError = true;
		}
		r.PropertyNameEnd = r.Position - 1;
		r.LastParsedPosition = r.Position;
		r.Statement = SelectorStatement.Value;
		r.ValueStart = r.Position;
		_ = r.TakeUntil(']');
		if (!Expect(ref r, ']'))
		{
			return;
		}
		r.ValueEnd = r.Position;
		r.Statement = SelectorStatement.Property;
		r.LastParsedPosition = r.Position;
		if (!r.End)
		{
			r.Statement = SelectorStatement.Middle;
		}
	}

	private static void ParseStart(ref ParserContext context)
	{
		context.SkipWhitespace();
		if (context.End)
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.End;
		}

		if (context.TakeIf(':'))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.Colon;
		}
		else if (context.TakeIf('.'))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.Class;
		}
		else if (context.TakeIf('#'))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.Name;
		}
		else if (context.TakeIf('^'))
		{
			context.LastParsedPosition = context.Position;
			context.Statement = SelectorStatement.CanHaveType;
		}
		else if (!context.End)
		{
			context.Statement = SelectorStatement.Middle;
		}
	}

	private static void ParseTemplate(ref ParserContext r)
	{
		var template = r.ParseIdentifier();
		const string templateKeyword = "template";
		if (!template.SequenceEqual(templateKeyword.AsSpan()))
		{
			r.LastParsedPosition = r.Position;
			r.IsError = true;
			return;
		}
		if (!r.TakeIf('/'))
		{
			r.LastParsedPosition = r.Position;
			r.IsError = true;
			return;
		}
		r.LastParsedPosition = r.Position;
		r.IsTemplate = true;
		(r.TemplateOwnerStart, r.TemplateOwnerEnd, r.NamespaceTemplateOwnerStart, r.NamespaceTemplateOwnerEnd) =
			(r.TypeNameStart, r.TypeNameEnd, r.NamespaceStart, r.NamespaceEnd);
		r.Statement = SelectorStatement.Start;
	}

	private static void ParseTraversal(ref ParserContext r)
	{
		r.SkipWhitespace();
		if (r.TakeIf('>'))
		{
			r.SkipWhitespace();
			r.Statement = SelectorStatement.Middle;
		}
		else if (r.TakeIf('/'))
		{
			r.LastParsedPosition = r.Position;
			r.Statement = SelectorStatement.Template;
		}
		else if (!r.End)
		{
			r.Statement = SelectorStatement.Middle;
		}
		else
		{
			r.LastParsedPosition = r.Position;
			r.Statement = SelectorStatement.End;
		}
	}

	private static void ParseType(ref ParserContext r)
	{
		r.LastParsedPosition = r.Position;
		ReadOnlySpan<char> ns = default;
		var startPosition = r.Position;
		var namespaceOrTypeName = r.ParseIdentifier();

		if (namespaceOrTypeName.IsEmpty)
		{
			r.IsError = true;
			return;
		}

		if (!r.End && r.TakeIf('|'))
		{
			ns = namespaceOrTypeName;
			r.NamespaceStart = startPosition;
			r.NamespaceEnd = r.Position - 1;
			if (r.End)
			{
				r.IsError = true;
				return;
			}
			r.TypeNameStart = r.Position;
			_ = r.ParseIdentifier();
			r.TypeNameEnd = r.Position;
		}
		else
		{
			r.TypeNameStart = startPosition;
			r.TypeNameEnd = r.Position;
		}
		r.LastParsedPosition = r.Position;
	}

	private static void ParseTypeName(ref ParserContext r)
	{
		ParseType(ref r);
		if (r.IsError)
		{
			return;
		}
		r.LastParsedPosition = r.Position;
		r.Statement = SelectorStatement.CanHaveType;
	}

	#endregion

	#region Structures

	private ref struct ParserContext
	{
		#region Fields

		public int ClassNameEnd = -1;
		public int ClassNameStart = -1;
		public int FunctionNameEnd = -1;
		public int FunctionNameStart = -1;
		public bool IsError = false;
		public bool IsTemplate;
		public int? LastParsedPosition = null;
		public int LastSegmentStartPosition;
		public int NameEnd = -1;
		public int NamespaceEnd = -1;
		public int NamespaceStart = -1;
		public int NamespaceTemplateOwnerEnd = -1;
		public int NamespaceTemplateOwnerStart = -1;
		public int NameStart = -1;
		public int PropertyNameEnd = -1;
		public int PropertyNameStart = -1;
		public int TemplateOwnerEnd = -1;
		public int TemplateOwnerStart = -1;
		public int TypeNameEnd = -1;
		public int TypeNameStart = -1;
		public int ValueEnd = -1;
		public int ValueStart = -1;
		private ReadOnlySpan<char> _data;
		private readonly ReadOnlySpan<char> _original;
		private SelectorStatement _statement = SelectorStatement.Start;

		#endregion

		#region Constructors

		public ParserContext(ReadOnlySpan<char> data) :
			this()
		{
			_data = data;
			_original = data;
		}

		#endregion

		#region Properties

		public bool End => _data.IsEmpty;

		public char Peek => _data[0];
		public int Position { get; private set; }

		public SelectorStatement PreviousStatement { get; private set; }

		public SelectorStatement Statement
		{
			get => _statement;
			set
			{
				if (_statement != value)
				{
					if (value is SelectorStatement.Start or SelectorStatement.Middle)
					{
						LastSegmentStartPosition = Position;
					}
					if (value is SelectorStatement.Start)
					{
						(NamespaceStart, NamespaceEnd, TypeNameStart, TypeNameEnd, ClassNameStart, ClassNameEnd, PropertyNameStart, PropertyNameEnd, NameStart, NameEnd, ValueStart, ValueEnd, FunctionNameStart, FunctionNameEnd) =
							(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1);
					}
					PreviousStatement = _statement;
				}
				_statement = value;
			}
		}

		#endregion

		#region Methods

		public ReadOnlySpan<char> GetRange(int from, int to)
		{
			if ((from < 0) || (from > _original.Length))
			{
				return ReadOnlySpan<char>.Empty;
			}
			if ((to < 0) || (to > _original.Length))
			{
				return _original.Slice(from);
			}
			return _original.Slice(from, to - from);
		}

		public ReadOnlySpan<char> ParseIdentifier()
		{
			if (!End && IsValidIdentifierStart(Peek))
			{
				return TakeWhile(c => IsValidIdentifierChar(c));
			}
			return ReadOnlySpan<char>.Empty;
		}

		public ReadOnlySpan<char> ParseStyleClass()
		{
			if (!End && IsValidIdentifierStart(Peek))
			{
				return TakeWhile(c => IsValidIdentifierChar(c));
			}
			return ReadOnlySpan<char>.Empty;
		}

		public ReadOnlySpan<char> PeekWhitespace()
		{
			var trimmed = _data.TrimStart();
			return _data.Slice(0, _data.Length - trimmed.Length);
		}

		public void Skip(int count)
		{
			if (_data.Length < count)
			{
				throw new IndexOutOfRangeException();
			}
			_data = _data.Slice(count);
		}

		public void SkipWhitespace()
		{
			var trimmed = _data.TrimStart();
			Position += _data.Length - trimmed.Length;
			_data = trimmed;
		}

		public char Take()
		{
			Position++;
			var take = _data[0];
			_data = _data.Slice(1);
			return take;
		}

		public bool TakeIf(char c)
		{
			if (!End && (Peek == c))
			{
				Take();
				return true;
			}
			return false;
		}

		public bool TakeIf(Func<char, bool> condition)
		{
			if (condition(Peek))
			{
				Take();
				return true;
			}
			return false;
		}

		public ReadOnlySpan<char> TakeUntil(char c)
		{
			int len;
			for (len = 0; (len < _data.Length) && (_data[len] != c); len++)
			{
			}
			var span = _data.Slice(0, len);
			_data = _data.Slice(len);
			Position += len;
			return span;
		}

		public ReadOnlySpan<char> TakeWhile(Func<char, bool> condition)
		{
			int len;
			for (len = 0; (len < _data.Length) && condition(_data[len]); len++)
			{
			}
			var span = _data.Slice(0, len);
			_data = _data.Slice(len);
			Position += len;
			return span;
		}

		public ReadOnlySpan<char> TryPeek(int count)
		{
			if (_data.Length < count)
			{
				return ReadOnlySpan<char>.Empty;
			}
			return _data.Slice(0, count);
		}

		private static bool IsValidIdentifierChar(char c)
		{
			if (IsValidIdentifierStart(c) || (c == '-'))
			{
				return true;
			}
			var cat = CharUnicodeInfo.GetUnicodeCategory(c);
			return (cat == UnicodeCategory.NonSpacingMark) ||
				(cat == UnicodeCategory.SpacingCombiningMark) ||
				(cat == UnicodeCategory.ConnectorPunctuation) ||
				(cat == UnicodeCategory.Format) ||
				(cat == UnicodeCategory.DecimalDigitNumber);
		}

		private static bool IsValidIdentifierStart(char c)
		{
			return char.IsLetter(c) || (c == '_');
		}

		#endregion
	}

	#endregion
}