using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.Parsing;

namespace Cornerstone.VisualStudio.Core.Completion;

/// <summary>
/// Resolves Go To Definition from a Cornerstone/Avalonia XAML caret position using
/// completion metadata. Does not navigate; the VS command handler does that.
/// </summary>
public static class XamlGoToDefinitionResolver
{
	#region Fields

	private static readonly Regex ClassAttributeRegex = new(
		@"\bx:Class\s*=\s*""([^""]+)""",
		RegexOptions.Compiled);

	#endregion

	#region Methods

	public static XamlGoToDefinitionTarget Resolve(
		CompletionEngine engine,
		Metadata metadata,
		string xml,
		int caret,
		string currentAssemblyName)
	{
		if ((engine == null) || string.IsNullOrEmpty(xml))
		{
			return XamlGoToDefinitionTarget.None;
		}

		caret = Math.Max(0, Math.Min(caret, xml.Length));
		if (metadata != null)
		{
			engine.Helper.SetMetadata(metadata, xml, currentAssemblyName);
		}

		var helper = engine.Helper;
		var parser = XmlParser.Parse(xml.AsMemory(), 0, caret);
		GetToken(xml, caret, out var token, out var tokenStart);
		var className = FindXamlClassName(xml);

		if (parser.State == XmlParser.ParserState.AttributeValue)
		{
			var attr = parser.AttributeName ?? "";
			if (IsSelectorAttribute(attr))
			{
				return ResolveSelector(helper, xml, caret, parser);
			}

			if (IsClassesAttribute(attr))
			{
				GetSelectorToken(xml, caret, out var classesToken, out _, out _);
				return ResolveStyleClass(xml, classesToken, caret);
			}

			if (IsClassAttribute(attr))
			{
				var value = FirstNonEmpty(TrimValue(parser.AttributeValue), token);
				return string.IsNullOrEmpty(value)
					? XamlGoToDefinitionTarget.None
					: XamlGoToDefinitionTarget.ForClass(value);
			}

			if (IsTypeNameAttribute(attr))
			{
				var fromToken = LookupType(helper, token);
				if (fromToken != null)
				{
					return XamlGoToDefinitionTarget.ForType(fromToken.FullName);
				}

				var value = TrimValue(parser.AttributeValue);
				fromToken = LookupType(helper, value);
				if (fromToken != null)
				{
					return XamlGoToDefinitionTarget.ForType(fromToken.FullName);
				}
			}

			var tagType = LookupType(helper, parser.TagName);
			if ((tagType != null) && IsEvent(tagType, attr))
			{
				var method = FirstNonEmpty(TrimValue(parser.AttributeValue), token);
				return string.IsNullOrEmpty(method)
					? XamlGoToDefinitionTarget.None
					: XamlGoToDefinitionTarget.ForMethod(method, className);
			}

			if ((parser.AttributeValue != null) && parser.AttributeValue.Contains("{"))
			{
				XamlGoToDefinitionTarget bindingTarget;
				if (TryResolveBindingPath(helper, parser, xml, caret, out bindingTarget))
				{
					return bindingTarget;
				}

				var extension = token.TrimStart('{');
				var markup = LookupType(helper, extension);
				if (markup != null)
				{
					return XamlGoToDefinitionTarget.ForType(markup.FullName);
				}
			}
			else
			{
				XamlGoToDefinitionTarget valueTarget;
				if (TryResolveAttributeValue(helper, parser, token, out valueTarget))
				{
					return valueTarget;
				}
			}
		}

		var name = FirstNonEmpty(token, parser.AttributeName, parser.ParseCurrentTagName(), parser.TagName);
		if (string.IsNullOrEmpty(name))
		{
			return XamlGoToDefinitionTarget.None;
		}

		return ResolveName(helper, parser.TagName, name, tokenStart, caret);
	}

	internal static void GetToken(string xml, int caret, out string token, out int tokenStart)
	{
		token = "";
		tokenStart = caret;
		if (string.IsNullOrEmpty(xml) || (xml.Length == 0))
		{
			return;
		}

		var index = caret;
		if ((index >= xml.Length) || !IsTokenChar(xml[index]))
		{
			index--;
		}

		if ((index < 0) || (index >= xml.Length) || !IsTokenChar(xml[index]))
		{
			return;
		}

		var start = index;
		while ((start > 0) && IsTokenChar(xml[start - 1]))
		{
			start--;
		}

		var end = index + 1;
		while ((end < xml.Length) && IsTokenChar(xml[end]))
		{
			end++;
		}

		tokenStart = start;
		token = xml.Substring(start, end - start);
	}

	private static string FindXamlClassName(string xml)
	{
		var match = ClassAttributeRegex.Match(xml);
		return match.Success ? match.Groups[1].Value : "";
	}

	private static string FirstNonEmpty(params string[] values)
	{
		if (values == null)
		{
			return "";
		}

		foreach (var value in values)
		{
			if (!string.IsNullOrEmpty(value))
			{
				return value;
			}
		}

		return "";
	}

	private static bool IsClassAttribute(string attributeName)
	{
		return attributeName.Equals("Class", StringComparison.Ordinal)
			|| attributeName.EndsWith(":Class", StringComparison.Ordinal);
	}

	private static bool IsClassesAttribute(string attributeName)
	{
		return attributeName.Equals("Classes", StringComparison.Ordinal);
	}

	private static bool IsSelectorAttribute(string attributeName)
	{
		return attributeName.Equals("Selector", StringComparison.Ordinal);
	}

	private static bool IsSelectorTokenChar(char c)
	{
		return char.IsLetterOrDigit(c) || (c == '_') || (c == '-') || (c == '|');
	}

	private static bool IsEvent(MetadataType type, string attributeName)
	{
		foreach (var item in type.Events)
		{
			if (string.Equals(item.Name, attributeName, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsTokenChar(char c)
	{
		return char.IsLetterOrDigit(c) || (c == '_') || (c == ':') || (c == '.');
	}

	private static bool IsTypeNameAttribute(string attributeName)
	{
		return attributeName.Equals("DataType", StringComparison.Ordinal)
			|| attributeName.EndsWith(":DataType", StringComparison.Ordinal)
			|| attributeName.Equals("Type", StringComparison.Ordinal)
			|| attributeName.EndsWith(":Type", StringComparison.Ordinal)
			|| attributeName.Equals("TargetType", StringComparison.Ordinal);
	}

	private static MetadataType LookupType(CompletionEngine.MetadataHelper helper, string name)
	{
		if (string.IsNullOrEmpty(name) || (helper == null))
		{
			return null;
		}

		return helper.LookupType(name);
	}

	private static bool MemberExists(MetadataType type, string memberName)
	{
		foreach (var property in type.Properties)
		{
			if (string.Equals(property.Name, memberName, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return IsEvent(type, memberName);
	}

	private static void GetSelectorToken(string xml, int caret, out string token, out int tokenStart, out char prefixChar)
	{
		token = "";
		tokenStart = caret;
		prefixChar = '\0';
		if (string.IsNullOrEmpty(xml) || (xml.Length == 0))
		{
			return;
		}

		var index = caret;
		if ((index >= xml.Length) || !IsSelectorTokenChar(xml[index]))
		{
			index--;
		}

		if ((index < 0) || (index >= xml.Length) || !IsSelectorTokenChar(xml[index]))
		{
			return;
		}

		var start = index;
		while ((start > 0) && IsSelectorTokenChar(xml[start - 1]))
		{
			start--;
		}

		var end = index + 1;
		while ((end < xml.Length) && IsSelectorTokenChar(xml[end]))
		{
			end++;
		}

		tokenStart = start;
		token = xml.Substring(start, end - start);
		if (start > 0)
		{
			prefixChar = xml[start - 1];
		}
	}

	private static string SelectorTypeName(SelectorParser selector)
	{
		var typeName = selector.TypeName;
		if (string.IsNullOrEmpty(typeName))
		{
			return "";
		}

		var ns = selector.Namespace;
		if (string.IsNullOrEmpty(ns))
		{
			return typeName;
		}

		return ns + ":" + typeName;
	}

	private static XamlGoToDefinitionTarget ResolveSelector(
		CompletionEngine.MetadataHelper helper,
		string xml,
		int caret,
		XmlParser parser)
	{
		GetSelectorToken(xml, caret, out var token, out var tokenStart, out var prefixChar);
		if (prefixChar == '.')
		{
			return ResolveStyleClass(xml, token, caret);
		}

		var lookupName = string.IsNullOrEmpty(token) ? "" : token.Replace('|', ':');
		var fromToken = LookupType(helper, lookupName);
		if (fromToken != null)
		{
			return XamlGoToDefinitionTarget.ForType(fromToken.FullName);
		}

		var valueStart = parser.CurrentValueStart ?? tokenStart;
		valueStart = Math.Max(0, Math.Min(valueStart, xml.Length));
		var parseEnd = tokenStart + token.Length;
		if (parseEnd < caret)
		{
			parseEnd = caret;
		}

		parseEnd = Math.Max(valueStart, Math.Min(parseEnd, xml.Length));
		var selectorText = xml.Substring(valueStart, parseEnd - valueStart);
		MetadataType selectorType = null;
		if (!string.IsNullOrEmpty(selectorText))
		{
			var selector = SelectorParser.Parse(selectorText.AsSpan());
			selectorType = LookupType(helper, SelectorTypeName(selector));
		}

		if ((selectorType != null) && !string.IsNullOrEmpty(token) && MemberExists(selectorType, token))
		{
			return XamlGoToDefinitionTarget.ForMember(selectorType.FullName, token);
		}

		if (selectorType != null)
		{
			return XamlGoToDefinitionTarget.ForType(selectorType.FullName);
		}

		if (string.IsNullOrEmpty(token) || (prefixChar == '.') || (prefixChar == ':') || (prefixChar == '#'))
		{
			return XamlGoToDefinitionTarget.None;
		}

		var simple = lookupName;
		var colon = simple.LastIndexOf(':');
		if (colon >= 0)
		{
			simple = simple.Substring(colon + 1);
		}

		return string.IsNullOrEmpty(simple)
			? XamlGoToDefinitionTarget.None
			: XamlGoToDefinitionTarget.ForClass(simple);
	}

	private static XamlGoToDefinitionTarget ResolveStyleClass(string xml, string className, int caret)
	{
		if (string.IsNullOrEmpty(className))
		{
			return XamlGoToDefinitionTarget.None;
		}

		var offset = StyleClassScanner.FindSelectorClassOffset(xml, className, caret);
		return XamlGoToDefinitionTarget.ForStyleClass(className, offset);
	}

	private static bool TryResolveAttributeValue(
		CompletionEngine.MetadataHelper helper,
		XmlParser parser,
		string token,
		out XamlGoToDefinitionTarget target)
	{
		target = XamlGoToDefinitionTarget.None;
		if (string.IsNullOrEmpty(token) || (helper == null) || (helper.Metadata == null))
		{
			return false;
		}

		var property = LookupAttributeProperty(helper, parser);
		var valueType = UnwrapNullable(property == null ? null : property.Type);
		if (valueType == null)
		{
			return false;
		}

		// Enum fields are stored as static getters on the enum. TextTrimming.None is the same shape.
		if (HasStaticGetter(valueType, token))
		{
			target = XamlGoToDefinitionTarget.ForMember(valueType.FullName, token);
			return true;
		}

		if (!HintContains(valueType, token) && !HintContains(property.Type, token))
		{
			return false;
		}

		var owner = FindHintOwner(helper.Metadata, token, valueType);
		if (owner == null)
		{
			return false;
		}

		target = XamlGoToDefinitionTarget.ForMember(owner.FullName, token);
		return true;
	}

	private static MetadataProperty LookupAttributeProperty(CompletionEngine.MetadataHelper helper, XmlParser parser)
	{
		var attributeName = parser.AttributeName ?? "";
		var dot = attributeName.LastIndexOf('.');
		if (dot > 0)
		{
			var attached = helper.LookupProperty(attributeName.Substring(0, dot), attributeName.Substring(dot + 1));
			if (attached != null)
			{
				return attached;
			}
		}

		return helper.LookupProperty(parser.TagName, attributeName);
	}

	private static MetadataType UnwrapNullable(MetadataType type)
	{
		if ((type != null) && type.IsNullable && (type.UnderlyingType != null))
		{
			return type.UnderlyingType;
		}

		return type;
	}

	private static bool HasStaticGetter(MetadataType type, string name)
	{
		if ((type == null) || (type.Properties == null))
		{
			return false;
		}

		for (var i = 0; i < type.Properties.Count; i++)
		{
			var property = type.Properties[i];
			if (property.IsStatic && property.HasGetter && string.Equals(property.Name, name, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static bool HintContains(MetadataType type, string name)
	{
		if ((type == null) || (type.HintValues == null))
		{
			return false;
		}

		for (var i = 0; i < type.HintValues.Length; i++)
		{
			if (string.Equals(type.HintValues[i], name, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static MetadataType FindHintOwner(Metadata metadata, string token, MetadataType valueType)
	{
		MetadataType best = null;
		var bestScore = 0;
		foreach (var types in metadata.Namespaces.Values)
		{
			foreach (var type in types.Values)
			{
				if (ReferenceEquals(type, valueType) || (type.Properties == null))
				{
					continue;
				}

				MetadataProperty match = null;
				for (var i = 0; i < type.Properties.Count; i++)
				{
					var property = type.Properties[i];
					if (property.IsStatic && property.HasGetter && string.Equals(property.Name, token, StringComparison.Ordinal))
					{
						match = property;
						break;
					}
				}

				if (match == null)
				{
					continue;
				}

				var score = ScoreReturnType(match.Type, valueType) + MatchingHintCount(type, valueType.HintValues);
				if (score > bestScore)
				{
					bestScore = score;
					best = match.DeclaringType ?? type;
				}
			}
		}

		// Score 1 is a same-named static with no type affinity. Leave those for Visual Studio.
		return bestScore > 1 ? best : null;
	}

	private static int MatchingHintCount(MetadataType type, string[] hints)
	{
		if ((hints == null) || (type.Properties == null))
		{
			return 0;
		}

		var count = 0;
		for (var i = 0; i < hints.Length; i++)
		{
			if (HasStaticGetter(type, hints[i]))
			{
				count++;
				if (count >= 30)
				{
					return 30;
				}
			}
		}

		return count;
	}

	private static int ScoreReturnType(MetadataType returnType, MetadataType valueType)
	{
		if ((returnType == null) || (valueType == null))
		{
			return 1;
		}

		if (ReferenceEquals(returnType, valueType) ||
			string.Equals(returnType.FullName, valueType.FullName, StringComparison.Ordinal))
		{
			return 100;
		}

		var returnName = returnType.Name ?? "";
		var targetName = valueType.Name ?? "";
		if ((returnName.Length == 0) || (targetName.Length == 0))
		{
			return 1;
		}

		if ((returnName.IndexOf(targetName, StringComparison.Ordinal) >= 0) ||
			(targetName.IndexOf(returnName, StringComparison.Ordinal) >= 0))
		{
			return 60;
		}

		if ((targetName.IndexOf("Brush", StringComparison.Ordinal) >= 0) &&
			(returnName.IndexOf("Brush", StringComparison.Ordinal) >= 0))
		{
			return 40;
		}

		return 1;
	}

	private static bool TryResolveBindingPath(
		CompletionEngine.MetadataHelper helper,
		XmlParser parser,
		string xml,
		int caret,
		out XamlGoToDefinitionTarget target)
	{
		target = XamlGoToDefinitionTarget.None;
		if ((parser == null) || string.IsNullOrEmpty(xml) || (parser.CurrentValueStart == null))
		{
			return false;
		}

		string path;
		int pathStart;
		if (!TryReadBindingPath(xml, parser.CurrentValueStart.Value, caret, out path, out pathStart))
		{
			return false;
		}

		BindingSegment[] segments;
		int segmentIndex;
		if (!TrySelectBindingSegment(path, caret - pathStart, out segments, out segmentIndex))
		{
			return false;
		}

		var dataType = LookupType(helper, parser.FindParentAttributeValue("(x\\:)?DataType"));
		if (dataType == null)
		{
			return false;
		}

		for (var i = 0; i < segmentIndex; i++)
		{
			var step = FindInstanceProperty(dataType, segments[i].Name);
			if ((step == null) || (step.Type == null))
			{
				return false;
			}

			dataType = step.Type;
		}

		var property = FindInstanceProperty(dataType, segments[segmentIndex].Name);
		if (property == null)
		{
			return false;
		}

		var ownerName = "";
		var owner = property.DeclaringType;
		if ((owner != null) && !string.IsNullOrEmpty(owner.FullName))
		{
			ownerName = owner.FullName;
		}
		else
		{
			ownerName = dataType.FullName;
		}

		if (string.IsNullOrEmpty(ownerName))
		{
			return false;
		}

		target = XamlGoToDefinitionTarget.ForMember(ownerName, property.Name);
		return true;
	}

	private static bool TryReadBindingPath(string xml, int valueStart, int caret, out string path, out int pathStart)
	{
		path = "";
		pathStart = 0;
		if ((valueStart < 0) || (valueStart >= xml.Length))
		{
			return false;
		}

		var valueEnd = valueStart;
		while ((valueEnd < xml.Length) && (xml[valueEnd] != '"') && (xml[valueEnd] != '<'))
		{
			valueEnd++;
		}

		var i = valueStart;
		while (i < valueEnd)
		{
			if (xml[i] != '{')
			{
				i++;
				continue;
			}

			var nameStart = i + 1;
			var nameEnd = nameStart;
			while ((nameEnd < valueEnd) && IsMarkupNameChar(xml[nameEnd]))
			{
				nameEnd++;
			}

			var extensionName = xml.Substring(nameStart, nameEnd - nameStart);
			if (!IsBindingExtensionName(extensionName))
			{
				i = nameEnd;
				continue;
			}

			var arg = nameEnd;
			var candidateStart = -1;
			var candidateEnd = -1;
			while (arg < valueEnd)
			{
				while ((arg < valueEnd) && char.IsWhiteSpace(xml[arg]))
				{
					arg++;
				}

				if ((arg >= valueEnd) || (xml[arg] == '}'))
				{
					break;
				}

				int argumentNameLength;
				if (LooksLikeNamedMarkupArgument(xml, arg, valueEnd, out argumentNameLength))
				{
					var argumentName = xml.Substring(arg, argumentNameLength);
					arg += argumentNameLength;
					while ((arg < valueEnd) && char.IsWhiteSpace(xml[arg]))
					{
						arg++;
					}

					if ((arg < valueEnd) && (xml[arg] == '='))
					{
						arg++;
					}

					while ((arg < valueEnd) && char.IsWhiteSpace(xml[arg]))
					{
						arg++;
					}

					var argumentValueStart = arg;
					var argumentValueEnd = ReadMarkupValue(xml, arg, valueEnd);
					if (argumentName.Equals("Path", StringComparison.OrdinalIgnoreCase))
					{
						candidateStart = argumentValueStart;
						candidateEnd = argumentValueEnd;
					}

					arg = argumentValueEnd;
				}
				else
				{
					var argumentValueStart = arg;
					var argumentValueEnd = ReadMarkupValue(xml, arg, valueEnd);
					if (candidateStart < 0)
					{
						candidateStart = argumentValueStart;
						candidateEnd = argumentValueEnd;
					}

					arg = argumentValueEnd;
				}

				while ((arg < valueEnd) && char.IsWhiteSpace(xml[arg]))
				{
					arg++;
				}

				if ((arg < valueEnd) && (xml[arg] == ','))
				{
					arg++;
					continue;
				}

				break;
			}

			if ((candidateStart >= 0) && (caret >= candidateStart) && (caret <= candidateEnd) && (candidateEnd > candidateStart))
			{
				pathStart = candidateStart;
				path = xml.Substring(candidateStart, candidateEnd - candidateStart);
				return true;
			}

			i = arg > i ? arg : nameEnd;
		}

		return false;
	}

	private static bool TrySelectBindingSegment(string path, int offset, out BindingSegment[] segments, out int index)
	{
		segments = new BindingSegment[0];
		index = -1;
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}

		if (offset < 0)
		{
			offset = 0;
		}

		if (offset > path.Length)
		{
			offset = path.Length;
		}

		var parsed = new List<BindingSegment>();
		var blocked = false;
		var i = 0;
		while ((i < path.Length) && (path[i] == '!'))
		{
			i++;
		}

		while (i < path.Length)
		{
			var c = path[i];
			if (c == '.')
			{
				i++;
				continue;
			}

			if ((c == '?') && ((i + 1) < path.Length) && (path[i + 1] == '.'))
			{
				i += 2;
				continue;
			}

			if (c == '[')
			{
				blocked = true;
				i = SkipBrackets(path, i);
				continue;
			}

			if (!IsBindingSegmentStart(c))
			{
				break;
			}

			var start = i;
			i++;
			while ((i < path.Length) && IsBindingSegmentChar(path[i]))
			{
				i++;
			}

			parsed.Add(new BindingSegment(path.Substring(start, i - start), start, i, !blocked));
		}

		if (parsed.Count == 0)
		{
			return false;
		}

		var selected = -1;
		for (var s = 0; s < parsed.Count; s++)
		{
			if ((offset >= parsed[s].Start) && (offset < parsed[s].End))
			{
				selected = s;
				break;
			}
		}

		// A caret sitting on '.' or '?.' belongs to the property before that separator.
		if (selected < 0)
		{
			for (var s = parsed.Count - 1; s >= 0; s--)
			{
				if (offset < parsed[s].End)
				{
					continue;
				}

				if (IsCaretOnSegmentBoundary(path, parsed[s].End, offset))
				{
					selected = s;
				}

				break;
			}
		}

		if ((selected < 0) || !parsed[selected].Reachable)
		{
			return false;
		}

		segments = parsed.ToArray();
		index = selected;
		return true;
	}

	private static bool IsCaretOnSegmentBoundary(string path, int segmentEnd, int offset)
	{
		if (offset == segmentEnd)
		{
			return true;
		}

		var i = segmentEnd;
		if ((i < path.Length) && (path[i] == '?'))
		{
			i++;
		}

		if ((i < path.Length) && (path[i] == '.'))
		{
			i++;
		}

		return offset <= i;
	}

	private static int SkipBrackets(string text, int index)
	{
		var depth = 0;
		while (index < text.Length)
		{
			var c = text[index];
			if (c == '[')
			{
				depth++;
				index++;
				continue;
			}

			if (c == ']')
			{
				depth--;
				index++;
				if (depth <= 0)
				{
					return index;
				}

				continue;
			}

			index++;
		}

		return index;
	}

	private static int ReadMarkupValue(string xml, int start, int end)
	{
		var i = start;
		while (i < end)
		{
			var c = xml[i];
			if (char.IsWhiteSpace(c) || (c == ',') || (c == '}') || (c == '{') || (c == '"'))
			{
				break;
			}

			i++;
		}

		return i;
	}

	private static bool LooksLikeNamedMarkupArgument(string xml, int index, int end, out int nameLength)
	{
		nameLength = 0;
		if ((index >= end) || !IsBindingSegmentStart(xml[index]))
		{
			return false;
		}

		var i = index + 1;
		while ((i < end) && IsBindingSegmentChar(xml[i]))
		{
			i++;
		}

		var length = i - index;
		while ((i < end) && char.IsWhiteSpace(xml[i]))
		{
			i++;
		}

		if ((i >= end) || (xml[i] != '='))
		{
			return false;
		}

		nameLength = length;
		return true;
	}

	private static bool IsBindingExtensionName(string name)
	{
		return name.Equals("Binding", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("x:Bind", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("Bind", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsBindingSegmentChar(char c)
	{
		return char.IsLetterOrDigit(c) || (c == '_');
	}

	private static bool IsBindingSegmentStart(char c)
	{
		return char.IsLetter(c) || (c == '_');
	}

	private static bool IsMarkupNameChar(char c)
	{
		return char.IsLetterOrDigit(c) || (c == '_') || (c == ':');
	}

	private static MetadataProperty FindInstanceProperty(MetadataType type, string name)
	{
		if ((type == null) || string.IsNullOrEmpty(name))
		{
			return null;
		}

		foreach (var property in type.Properties)
		{
			if (property.IsAttached || property.IsStatic || !property.HasGetter)
			{
				continue;
			}

			if (string.Equals(property.Name, name, StringComparison.Ordinal))
			{
				return property;
			}
		}

		return null;
	}

	private static XamlGoToDefinitionTarget ResolveName(
		CompletionEngine.MetadataHelper helper,
		string tagName,
		string name,
		int tokenStart,
		int caret)
	{
		var dot = name.LastIndexOf('.');
		if (dot > 0)
		{
			var left = name.Substring(0, dot);
			var right = name.Substring(dot + 1);
			var owner = LookupType(helper, left);
			if (owner != null)
			{
				var caretOnLeft = caret < (tokenStart + dot);
				if (caretOnLeft || !MemberExists(owner, right))
				{
					return XamlGoToDefinitionTarget.ForType(owner.FullName);
				}

				return XamlGoToDefinitionTarget.ForMember(owner.FullName, right);
			}
		}

		var type = LookupType(helper, name);
		if (type != null)
		{
			return XamlGoToDefinitionTarget.ForType(type.FullName);
		}

		var tagType = LookupType(helper, tagName);
		if ((tagType != null) && MemberExists(tagType, name))
		{
			return XamlGoToDefinitionTarget.ForMember(tagType.FullName, name);
		}

		if (!string.IsNullOrEmpty(name) && name.Contains("."))
		{
			return XamlGoToDefinitionTarget.ForClass(name);
		}

		var simple = name;
		var colon = name.LastIndexOf(':');
		if (colon >= 0)
		{
			simple = name.Substring(colon + 1);
		}

		return string.IsNullOrEmpty(simple)
			? XamlGoToDefinitionTarget.None
			: XamlGoToDefinitionTarget.ForClass(simple);
	}

	private static string TrimValue(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "";
		}

		value = value.Trim();
		if ((value.Length > 0) && ((value[0] == '"') || (value[0] == '\'')))
		{
			value = value.Substring(1);
		}

		var quote = value.IndexOfAny(['"', '\'']);
		if (quote >= 0)
		{
			value = value.Substring(0, quote);
		}

		return value.Trim();
	}

	#endregion

	#region Structures

	private readonly struct BindingSegment
	{
		public BindingSegment(string name, int start, int end, bool reachable)
		{
			Name = name;
			Start = start;
			End = end;
			Reachable = reachable;
		}

		public int End { get; }

		public string Name { get; }

		public bool Reachable { get; }

		public int Start { get; }
	}

	#endregion
}
