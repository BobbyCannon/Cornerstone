#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.NamespaceTransformations;
using Cornerstone.VisualStudio.Core.Parsing;

#endregion

namespace Cornerstone.VisualStudio.Core.Completion;

public class CompletionEngine
{
	#region Fields

	public static readonly IEnumerable<INamespaceTransformation> Default =
	[
		new ToLowerTransformation(),
		new ReplaceDot('_')
	];

	#endregion

	#region Properties

	public MetadataHelper Helper { get; set; } = new();

	#endregion

	#region Methods

	public IEnumerable<string> FilterHintValues(MetadataType type, string? entered, string? currentAssemblyName, XmlParser? state)
	{
		entered ??= "";

		if (type == null)
		{
			yield break;
		}

		if (!string.IsNullOrEmpty(currentAssemblyName) && (type.XamlContextHintValuesFunc != null))
		{
			foreach (var v in type.XamlContextHintValuesFunc(currentAssemblyName, type, null).Where(v => v.StartsWith(entered, StringComparison.OrdinalIgnoreCase)))
			{
				yield return v;
			}
		}

		if (type.HintValues is not null)
		{
			// Don't filter values here by 'StartsWith' (old behavior), provide all the hints,
			// For VS, Intellisense will filter the results for us, other users of the completion
			// engine (outside VS) will need to filter later
			// Otherwise, in VS, it's impossible to get the full list for something like brushes:
			// Background="Red" -> Background="B", will only populate with the 'B' brushes and hitting
			// backspace after that will keep the 'B' brushes only instead of showing the whole list
			// WPF/UWP loads the full list of brushes and highlights starting at the B and then
			// filters the list down from there - otherwise it is difficult to keep the completion list
			// and see all choices if making edits
			foreach (var v in type.HintValues)
			{
				yield return v;
			}
		}
	}

	public static CompletionKind GetCompletionKindForHintValues(MetadataType type)
	{
		return type.IsEnum ? CompletionKind.Enum : CompletionKind.StaticProperty;
	}

	public CompletionSet? GetCompletions(Metadata metadata, string fullText, int pos, string? currentAssemblyName = null)
	{
		var textToCursor = fullText.Substring(0, pos);
		Helper.SetMetadata(metadata, textToCursor, currentAssemblyName);

		if (Helper.Metadata == null)
		{
			return null;
		}

		if ((fullText.Length == 0) || (pos == 0))
		{
			return null;
		}

		var state = XmlParser.Parse(textToCursor);
		var completions = new List<Completion>();
		var curStart = state.CurrentValueStart ?? 0;

		switch (state.State)
		{
			case XmlParser.ParserState.StartElement:
			{
				var tagName = state.TagName;
				if (tagName is null)
				{
				}
				else if (tagName.StartsWith("/"))
				{
					if (textToCursor.Length < 2)
					{
						return null;
					}
					var closingState = XmlParser.Parse(textToCursor.Substring(0, textToCursor.Length - 2));

					var name = closingState.GetParentTagName(0);
					if (name == null)
					{
						return null;
					}
					completions.Add(new Completion("/" + name + ">", CompletionKind.Class, priority: 0));
				}
				else if (tagName.Contains('.'))
				{
					var dotPos = tagName.IndexOf(".");
					var typeName = tagName.Substring(0, dotPos);
					var compName = tagName.Substring(dotPos + 1);
					curStart = curStart + dotPos + 1;

					var sameType = state.GetParentTagName(1) == typeName;

					completions.AddRange(Helper.FilterPropertyNames(typeName, compName, sameType ? null : true, false)
						.Select(p => new Completion(p, sameType ? CompletionKind.Property : CompletionKind.AttachedProperty)));
				}
				else
				{
					if (tagName.Length == 0)
					{
						if (state.GetParentTagName(1) is string parentTag)
						{
							if (!state.IsInClosingTag)
							{
								completions.Add(new Completion("/" + parentTag + ">", CompletionKind.Class, priority: 0));
							}
							if (parentTag.IndexOf('.') == -1)
							{
								completions.Add(new Completion(parentTag, $"{parentTag}.", CompletionKind.Class, priority: 1)
								{
									TriggerCompletionAfterInsert = true
								});
							}
						}
						completions.Add(new Completion("!--", "!---->", CompletionKind.Comment) { RecommendedCursorOffset = 3 });
					}
					completions.AddRange(Helper.FilterTypes(tagName)
						.Where(kvp => !kvp.Value.IsAbstract)
						.Select(kvp =>
						{
							var ci = GetElementCompletionInfo(kvp.Key, kvp.Value);
							// Pass RecommendedCursorOffset via primary ctor (not object-initializer)
							// so XamlCompletion always gets the caret index within InsertText.
							return new Completion(
								ci.DisplayText,
								ci.InsertText,
								ci.DisplayText,
								CompletionKind.Class,
								ci.RecommendedCursorOffset)
							{
								TriggerCompletionAfterInsert = ci.TriggerCompletionAfterInsert
							};
						}));
				}
				break;
			}
			case XmlParser.ParserState.InsideElement
				or XmlParser.ParserState.StartAttribute:
			{
				if (state.State == XmlParser.ParserState.InsideElement)
				{
					curStart = pos; //Force completion to be started from current cursor position
				}

				var attributeSuffix = "=\"\"";
				var attributeOffset = 2;
				if ((fullText.Length > pos) && (fullText[pos] == '='))
				{
					// attribute already has value, we are editing name only
					attributeSuffix = "";
					attributeOffset = 0;
				}
				var attributeName = state.AttributeName;
				if (attributeName?.Contains('.') == true)
				{
					var dotPos = attributeName.IndexOf('.');
					curStart += dotPos + 1;
					var split = attributeName.Split(['.'], 2);
					completions.AddRange(Helper.FilterPropertyNames(split[0], split[1], true, true)
						.Select(x => new Completion(x, x + attributeSuffix, x, CompletionKind.AttachedProperty, x.Length + attributeOffset)));

					completions.AddRange(Helper.FilterEventNames(split[0], split[1], true)
						.Select(v => new Completion(v, v + attributeSuffix, v, CompletionKind.AttachedEvent, v.Length + attributeOffset)));
				}
				else if (state.TagName is not null)
				{
					completions.AddRange(Helper.FilterPropertyNames(state.TagName, attributeName, false, true)
						.Select(x => new Completion(x, x + attributeSuffix, x, CompletionKind.Property, x.Length + attributeOffset)));

					// Special case for "<On " here, 'Options' property is get only list property
					// which is skipped above - Add it back here
					// Future TODO: The metadata probably needs to adapt for this, but this opens up
					// potential issues with readonly properties that we don't want visible, so leaving
					// this up to be dealt with in the future
					if (state.TagName.Equals("On"))
					{
						completions.Add(new Completion("Options", "Options=\"\"", "Options",
							CompletionKind.Property, 9 /*recommendedCursorOffset*/));
					}

					completions.AddRange(Helper.FilterEventNames(state.TagName, attributeName, false)
						.Select(v => new Completion(v, v + attributeSuffix, v, CompletionKind.Event, v.Length + attributeOffset)));

					var targetType = Helper.LookupType(state.TagName);
					if (targetType is not null)
					{
						completions.AddRange(
							Helper.FilterTypes(attributeName, xamlDirectiveOnly: true)
								.Where(t => t.Value.IsValidForXamlContextFunc?.Invoke(currentAssemblyName, targetType, null) ?? true)
								.Select(v => new Completion(v.Key, v.Key + attributeSuffix, v.Key, CompletionKind.Class, v.Key.Length + attributeOffset)));

						if (targetType.IsAvaloniaObjectType)
						{
							if (string.IsNullOrEmpty(attributeName) || "xmlns".StartsWith(attributeName, StringComparison.OrdinalIgnoreCase))
							{
								completions.Add(new("xmlns:", CompletionKind.Class));
							}
							completions.AddRange(
								Helper.FilterTypeNames(attributeName, true)
									.Select(v => new Completion(v, v + ".", v, CompletionKind.Class)));
						}
					}
				}
				break;
			}
			case XmlParser.ParserState.AttributeValue:
			{
				var type = Helper.LookupType(state.TagName);

				MetadataProperty? prop = null;
				if (state.AttributeName?.Contains('.') == true)
				{
					//Attached property
					var split = state.AttributeName.Split('.');
					prop = Helper.LookupProperty(split[0], split[1]);
				}
				else if (state.TagName is not null)
				{
					prop = Helper.LookupProperty(state.TagName, state.AttributeName);
				}

				//Markup extension, ignore everything else
				if ((state.AttributeValue?.StartsWith("{") == true) && state.CurrentValueStart.HasValue)
				{
					curStart = state.CurrentValueStart.Value +
						BuildCompletionsForMarkupExtension(prop, completions, fullText, state,
							textToCursor.Substring(state.CurrentValueStart.Value), currentAssemblyName);
				}
				else if ((type != null) && (type.Events.FirstOrDefault(x => x.Name == state.AttributeName) != null))
				{
					var name = state.TagName!;
					// Clean up xmlns
					var index = name.IndexOf(':');
					if (index > -1)
					{
						name = name.Substring(index + 1, name.Length - index - 1);
					}
					completions.Add(new Completion("<New Event Handler>", $"{name}_{state.AttributeName}", CompletionKind.StaticProperty));
				}
				else
				{
					prop ??= Helper.LookupType(state.AttributeName)?.Properties.FirstOrDefault(p => string.IsNullOrEmpty(p.Name));

					if ((prop?.Type?.HasHintValues == true) && state.CurrentValueStart.HasValue)
					{
						var search = textToCursor.Substring(state.CurrentValueStart.Value);
						var hintCompletions = true;
						if (prop.Type.IsCompositeValue)
						{
							// Special case for pseudoclasses within the current edit
							if (state.AttributeName!.Equals("Selector"))
							{
								hintCompletions = false;
								if (ProcessSelector(search.AsSpan(), state, completions, currentAssemblyName, fullText) is int delta)
								{
									curStart = curStart + delta;
								}
							}
							else
							{
								var last = search.Split(' ', ',').Last();
								search = last;
								curStart = (curStart + search.Length) - last?.Length ?? 0;
							}
						}
						if (hintCompletions)
						{
							completions.AddRange(GetHintCompletions(prop.Type, search, currentAssemblyName));
						}
					}
					else if (prop?.Type?.Name == typeof(Type).FullName)
					{
						var cKind = CompletionKind.Class;
						if ((state.AttributeName?.Equals("TargetType") == true)
							|| (state.AttributeName?.Equals("Selector") == true))
						{
							cKind |= CompletionKind.TargetTypeClass;
						}

						completions.AddRange(Helper.FilterTypeNames(state?.AttributeValue)
							.Select(x => new Completion(x, x, x, cKind)));
					}
					else if (((state.AttributeName == "xmlns")
								|| (state.AttributeName?.Contains("xmlns:") == true))
							&& state.AttributeValue is not null)
					{
						IEnumerable<string> FilterNamespaces(Func<string, bool> predicate)
						{
							var result = metadata.Namespaces.Keys.Where(predicate).ToList();

							result.Sort((x, y) => x.CompareTo(y));

							return result;
						}

						var cKind = CompletionKind.Namespace | CompletionKind.VsXmlns;

						if (state.AttributeValue.StartsWith("clr-namespace:"))
						{
							completions.AddRange(
								FilterNamespaces(v => v.StartsWith(state.AttributeValue))
									.Select(v => new Completion(v.Substring("clr-namespace:".Length), v, v, cKind)));
						}
						else
						{
							if ("using:".StartsWith(state.AttributeValue))
							{
								completions.Add(new Completion("using:", cKind));
							}

							if ("clr-namespace:".StartsWith(state.AttributeValue))
							{
								completions.Add(new Completion("clr-namespace:", cKind));
							}

							completions.AddRange(
								FilterNamespaces(v =>
										v.StartsWith(state.AttributeValue) &&
										!v.StartsWith("clr-namespace"))
									.Select(v => new Completion(v, cKind)));
						}
					}
					else if ((state.AttributeName?.EndsWith(":Class") == true) && state.AttributeValue is not null)
					{
						if ((Helper.Aliases?.TryGetValue(state.AttributeName.Replace(":Class", ""), out var ns) == true) && (ns == Utils.Xaml2006Namespace))
						{
							var asmKey = $";assembly={currentAssemblyName}";
							var fullClassNames = Helper.Metadata.Namespaces.Where(v => v.Key.EndsWith(asmKey))
								.SelectMany(v => v.Value.Values.Where(t => t.IsAvaloniaObjectType))
								.Select(v => v.FullName);
							completions.AddRange(
								fullClassNames
									.Where(v => v.StartsWith(state.AttributeValue))
									.Select(v => new Completion(v, CompletionKind.Class | CompletionKind.TargetTypeClass)));
						}
					}
					else if ((state.TagName == "Setter") && ((state.AttributeName == "Value") || (state.AttributeName == "Property")))
					{
						ProcessStyleSetter(state.AttributeName, state, completions, currentAssemblyName);

						var isAttached = textToCursor.AsSpan().Slice(curStart, pos - curStart).IndexOf('.') != -1;
						if (isAttached)
						{
							curStart = pos;
						}
					}
					else if (state.TagName == "On")
					{
						if (state?.AttributeName?.Equals("Options") == true)
						{
							var parentTag = state.GetParentTagName(1);
							if (parentTag?.Equals("OnPlatform") == true)
							{
								// Built in types from:
								//https://github.com/AvaloniaUI/Avalonia/blob/master/src/Markup/Avalonia.Markup.Xaml/MarkupExtensions/OnPlatformExtension.cs
								completions.Add(new Completion("Windows", CompletionKind.Enum));
								completions.Add(new Completion("macOS", CompletionKind.Enum));
								completions.Add(new Completion("Linux", CompletionKind.Enum));
								completions.Add(new Completion("Android", CompletionKind.Enum));
								completions.Add(new Completion("IOS", CompletionKind.Enum));
								completions.Add(new Completion("Browser", CompletionKind.Enum));
							}
							else if (parentTag?.Equals("OnFormFactor") == true)
							{
								completions.Add(new Completion("Desktop", CompletionKind.Enum));
								completions.Add(new Completion("Mobile", CompletionKind.Enum));
							}
						}
						else if (state?.AttributeName?.Equals("Content") == true)
						{
							// For content, lets find the completions relevant to the property
							var propertyTag = state.GetParentTagName(2)!;
							var dotPos = propertyTag.IndexOf(".");
							var typeName = propertyTag.Substring(0, dotPos);
							var compName = propertyTag.Substring(dotPos + 1);

							var property = Helper.LookupProperty(typeName, compName);

							if (property?.Type?.HasHintValues == true)
							{
								completions.AddRange(GetHintCompletions(property.Type, null, currentAssemblyName));
							}
						}
					}
				}
				break;
			}
		}

		if (completions.Count != 0)
		{
			return new CompletionSet { Completions = SortCompletions(completions), StartPosition = curStart };
		}

		return null;
	}

	public static Dictionary<string, string> GetNamespaceAliases(string xml)
	{
		var rv = new Dictionary<string, string>();
		try
		{
			var xmlRdr = XmlReader.Create(new StringReader(xml));
			var result = true;
			while (result && (xmlRdr.NodeType != XmlNodeType.Element))
			{
				try
				{
					result = xmlRdr.Read();
				}
				catch
				{
					if (xmlRdr.NodeType != XmlNodeType.Element)
					{
						result = false;
					}
				}
			}

			if (result)
			{
				for (var c = 0; c < xmlRdr.AttributeCount; c++)
				{
					xmlRdr.MoveToAttribute(c);
					var ns = xmlRdr.Name;
					if ((ns != "xmlns") && !ns.StartsWith("xmlns:"))
					{
						continue;
					}
					ns = ns == "xmlns" ? "" : ns.Substring(6);
					rv[ns] = xmlRdr.Value;
				}
			}
		}
		catch
		{
			//
		}
		if (!rv.ContainsKey(""))
		{
			rv[""] = Utils.AvaloniaNamespace;
		}
		return rv;
	}

	public static string GetXmlnsFromNamespace(string @namespace)
	{
		return GetXmlnsFromNamespace(@namespace.ToCharArray(), Default);
	}

	public static string GetXmlnsFromNamespace(char[] @namespace)
	{
		return GetXmlnsFromNamespace(@namespace, Default);
	}

	public static string GetXmlnsFromNamespace(char[] @namespace, IEnumerable<INamespaceTransformation> trasformations)
	{
		IEnumerable<char> source = @namespace;
		foreach (var trasformation in trasformations)
		{
			source = trasformation.Apply(source);
		}
		return string.Concat(source);
	}

	public int? ProcessSelector(ReadOnlySpan<char> text, XmlParser state, List<Completion> completions, string? currentAssemblyName, string? fullText)
	{
		int? parsed = null;
		var parser = SelectorParser.Parse(text);
		var previousStatement = parser.PreviousStatement;
		switch (parser.Statement)
		{
			case SelectorStatement.Colon:
			case SelectorStatement.FunctionArgs:
			{
				var fn = parser.FunctionName;
				var tn = GetFullName(parser);
				var isEmptyTn = string.IsNullOrEmpty(tn);
				if ((previousStatement <= SelectorStatement.Middle) && isEmptyTn)
				{
					completions.Add(new Completion(":is()", ":is(", CompletionKind.Selector | CompletionKind.Enum));
				}
				else if (string.IsNullOrEmpty(fn))
				{
					completions.Add(new Completion(":not()", ":not(", CompletionKind.Selector | CompletionKind.Enum));
					completions.Add(new Completion(":nth-child()", ":nth-child(", CompletionKind.Selector | CompletionKind.Enum));
					completions.Add(new Completion(":nth-last-child()", ":nth-last-child(", CompletionKind.Selector | CompletionKind.Enum));
				}
				if (isEmptyTn)
				{
					var pseudoClasses = Helper.FilterTypes(null)
						.Select(kvp => kvp.Value)
						.Where(m => m.HasPseudoClasses)
						.SelectMany(m => m.PseudoClasses)
						.Distinct(StringComparer.OrdinalIgnoreCase);
					completions.AddRange(pseudoClasses.Select(v => new Completion(v, CompletionKind.Selector | CompletionKind.Enum)));
				}
				else
				{
					if (Helper.LookupType(tn) is { HasPseudoClasses: true } type)
					{
						completions.AddRange(type.PseudoClasses.Select(v => new Completion(v, CompletionKind.Selector | CompletionKind.Enum)));
					}
				}
				if (fn == "is")
				{
					var types = Helper
						.FilterTypes(null)
						.Where(t => t.Value.IsAvaloniaObjectType)
						.Select(t => t.Value)
						.ToList();

					if (types.Count != 0)
					{
						parsed = text.Length - (parser.LastParsedPosition + 1);
						completions.AddRange(types.Select(v =>
						{
							var name = GetXmlnsFullName(v);
							return new Completion(name, name + ".", CompletionKind.Class | CompletionKind.TargetTypeClass);
						}));
					}
				}
				if (completions.Count > 0)
				{
					parsed = parser.LastParsedPosition ?? 0;
				}
			}
				break;
			case SelectorStatement.Name:
			{
				if (parser.IsTemplate)
				{
					var ton = parser.TemplateOwner;
					if (string.IsNullOrEmpty(ton))
					{
						ton = GetTypeFromControlTheme();
					}
					if (!string.IsNullOrEmpty(ton))
					{
						//If it hat TemplateOwner 
						if (Helper.FilterTypes(ton)
								.Where(kvp => kvp.Value.TemplateParts.Any())
								.Select(kvp => kvp.Value)
								.FirstOrDefault() is { } ownerType)
						{
							var parts = ownerType.TemplateParts;
							var fullName = GetFullName(parser);
							var partType = string.IsNullOrEmpty(fullName)
								? null
								: Helper.FilterTypes(fullName)
									.Select(kvp => kvp.Value)
									.FirstOrDefault();
							if (partType is not null)
							{
								parts = parts
									.Where(p => p.Type.AssemblyQualifiedName == partType.AssemblyQualifiedName)
									.ToList();
							}
							if (parts.Any())
							{
								parsed = parser.LastParsedPosition ?? 0;
								var x = (parser.LastParsedPosition ?? 0) - parser.LastSegmentStartPosition - 1;
								if (!string.IsNullOrEmpty(fullName))
								{
									x += fullName.Length + 1;
								}
								completions.AddRange(parts!.Select(p => new Completion(p.Name, CompletionKind.Name | CompletionKind.Class, p.Type?.Name)
								{
									RecommendedCursorOffset = p.Name.Length + (p.Type?.Name.Length > 0 ? p.Type.Name.Length + 3 : 0),
									DeleteTextOffset = -x
								}));
							}
						}
					}
				}
				else if (fullText is not null)
				{
					var nameMatch = MetadataHelper
						.FindElementByNameRegex
						.Matches(fullText);
					if (nameMatch is { Count: > 0 })
					{
						var filterName = nameMatch.OfType<Match>();
						var elementName = parser.ElementName;
						if (!string.IsNullOrEmpty(elementName))
						{
							filterName = filterName
								.Where(m => m.Groups["AttribValue"].Value.StartsWith(elementName, StringComparison.OrdinalIgnoreCase));
						}
						foreach (var m in filterName)
						{
							if (m.Success)
							{
								parsed = parser.LastParsedPosition ?? 0;
								var name = m.Groups["AttribValue"].Value;
								completions.Add(new Completion(name, CompletionKind.Name | CompletionKind.Class));
							}
						}
					}
				}
			}
				break;
			case SelectorStatement.CanHaveType:
			case SelectorStatement.TypeName:
			{
				var tn = parser.TypeName;
				if (GetFullName(parser) is string typeFullName)
				{
					var len = typeFullName.Length;
					if (len > 0)
					{
						if (typeFullName[len - 1] == ':')
						{
							var ns = typeFullName.Substring(0, len - 1);

							if ((Helper.Aliases?.TryGetValue(ns!, out var ans) == true)
								&& (Helper.Metadata?.Namespaces.TryGetValue(ans, out var types) == true))
							{
								IEnumerable<MetadataType> ft = types.Values;
								ft = ft
									.Where(t => !t.IsGeneric)
									.Where(t => !t.IsMarkupExtension)
									.Where(t => t.IsAvaloniaObjectType || t.HasAttachedProperties);
								completions.AddRange(ft.Select(v => new Completion(v.Name, $"{ns}|{v.Name}", CompletionKind.Class | CompletionKind.TargetTypeClass)));
								parsed = (parser.LastParsedPosition ?? 0) - (tn?.Length ?? 0);
							}
						}
						else if (Helper.FilterTypes(typeFullName).Select(kvp => kvp.Value) is { } types)
						{
							types = types
								.Where(t => !t.IsGeneric)
								.Where(t => !t.IsMarkupExtension)
								.Where(t => t.IsAvaloniaObjectType || t.HasAttachedProperties);
							completions.AddRange(types.Select(v =>
							{
								var name = GetXmlnsFullName(v);
								return new Completion(name, CompletionKind.Class | CompletionKind.TargetTypeClass);
							}));
							parsed = (parser.LastParsedPosition ?? 0) - (tn?.Length ?? 0);
						}
					}
				}
			}
				break;
			case SelectorStatement.Property:
			{
				var typeFullName = GetFullName(parser);
				if (Helper.LookupType(typeFullName) is MetadataType type)
				{
					var propertyName = parser.PropertyName;
					var selectorElementProperties = MetadataHelper.FilterProperty(type, propertyName, null, false).ToList();
					if (selectorElementProperties.Count != 0)
					{
						parsed = (parser.LastParsedPosition ?? 0) - (propertyName?.Length ?? 0);
						completions.AddRange(selectorElementProperties.Select(v => new Completion(v.Name, v.Name + "=", v.IsAttached ? CompletionKind.AttachedProperty : CompletionKind.Property)));
					}
				}
			}
				break;
			case SelectorStatement.AttachedProperty:
			{
				var typeFullName = GetFullName(parser);
				if (Helper.LookupType(typeFullName) is { HasAttachedProperties: true } type)
				{
					var propertyName = parser.PropertyName;
					var selectorElementProperties = MetadataHelper.FilterProperty(type,
						propertyName,
						true,
						false
					);
					if (selectorElementProperties?.Any() == true)
					{
						var lenPropertyName = propertyName?.Length ?? 0;
						var lenType = (lenPropertyName == 0) || typeFullName is null
							? 0
							: typeFullName.Length + 1;
						parsed = ((parser.LastParsedPosition ?? 0) - lenType - lenType) + 1;
						completions.AddRange(selectorElementProperties.Select(v => new Completion(v.Name, v.Name + ")", v.IsAttached ? CompletionKind.AttachedProperty : CompletionKind.Property)));
					}
				}
				else
				{
					var types = Helper.FilterTypes(null)
						.Where(t => t.Value.HasAttachedProperties)
						.Select(t => t.Value);
					if (types?.Any() == true)
					{
						parsed = (parser.LastParsedPosition ?? 0) + 1;
						completions.AddRange(types.Select(v =>
						{
							var name = GetXmlnsFullName(v);
							return new Completion(name, name + ".", CompletionKind.Class);
						}));
					}
				}
			}
				break;
			case SelectorStatement.Template:
			{
				completions.Add(new("/template/", "/template/", CompletionKind.Selector | CompletionKind.Enum));
				parsed = parser.LastParsedPosition;
			}
				break;
			case SelectorStatement.Traversal:
			case SelectorStatement.Start:
			{
				if (!parser.IsError)
				{
					parsed = parser.LastParsedPosition ?? 0;
					var parent = state.GetParentTagName(1);
					// TODO: Crowling Selector operator from Attribute of the Selector
					completions.Add(new Completion("^", CompletionKind.Selector | CompletionKind.Enum));
					if (!string.Equals(parent, "ControlTheme", StringComparison.OrdinalIgnoreCase))
					{
						completions.Add(new Completion(":", CompletionKind.Selector | CompletionKind.Enum));
						completions.Add(new Completion(">", CompletionKind.Selector | CompletionKind.Enum));
						completions.Add(new Completion(".", CompletionKind.Selector | CompletionKind.Enum));
						completions.Add(new Completion("#", CompletionKind.Selector | CompletionKind.Enum));
						completions.Add(new Completion(":is()", ":is(", CompletionKind.Selector | CompletionKind.Enum));
						completions.Add(new Completion(":not()", ":not(", CompletionKind.Selector | CompletionKind.Enum));
						completions.Add(new Completion(":nth-child()", ":nth-child(", CompletionKind.Selector | CompletionKind.Enum));
						completions.Add(new Completion(":nth-last-child()", ":nth-last-child(", CompletionKind.Selector | CompletionKind.Enum));
						completions.Add(new Completion("/template/", "/template/", CompletionKind.Selector | CompletionKind.Enum));
						var types = Helper.FilterTypes(null)
							.Where(t => t.Value.IsAvaloniaObjectType || t.Value.HasAttachedProperties)
							.Select(t => new Completion(t.Value.Name.Replace(":", "|"), CompletionKind.Class | CompletionKind.TargetTypeClass));
						completions.AddRange(types);
					}
				}
			}
				break;
			case SelectorStatement.Value:
			{
				var typeFullName = GetFullName(parser);
				if (Helper.LookupType(typeFullName) is MetadataType type)
				{
					var propertyName = parser.PropertyName;
					var prop = MetadataHelper.FilterProperty(type,
						propertyName,
						null,
						false
					).FirstOrDefault();
					var propType = prop?.Type;
					if (propType?.IsNullable == true)
					{
						propType = propType.UnderlyingType;
					}
					if (propType is { HasHintValues: true } pt)
					{
						var kind = pt.IsEnum
							? CompletionKind.Enum
							: CompletionKind.StaticProperty;
						IEnumerable<string> values = pt.HintValues!;
						var value = parser.Value;
						if (!string.IsNullOrEmpty(value))
						{
							values = values
								.Where(v => v.StartsWith(value, StringComparison.OrdinalIgnoreCase));
						}
						completions.AddRange(values.Select(v => new Completion(v, kind)));
						parsed = parser.LastParsedPosition - (parser.Value?.Length ?? 0);
					}
				}
			}
				break;
			case SelectorStatement.Function:
			case SelectorStatement.Class:
			case SelectorStatement.Middle:
			case SelectorStatement.End:
			default:
				break;
		}
		return parsed;

		string GetFullName(SelectorParser parser)
		{
			var ns = parser.Namespace;
			var typename = parser.TypeName;
			if (string.IsNullOrEmpty(typename))
			{
				typename = GetTypeFromControlTheme();
			}
			var typeFullName = string.IsNullOrEmpty(ns)
				? typename
				: $"{ns}:{typename}";
			return typeFullName ?? string.Empty;
		}

		string GetXmlnsFullName(MetadataType type, char namespaceSeparator = '|')
		{
			if ((Helper.Metadata?.InverseNamespace.TryGetValue(type.FullName, out var ns) == true)
				&& !string.IsNullOrEmpty(ns))
			{
				var alias = Helper.Aliases?.FirstOrDefault(a => Equals(a.Value, ns));
				if (alias is not null && !string.IsNullOrEmpty(alias.Value.Key))
				{
					return $"{alias.Value.Key}{namespaceSeparator}{type.Name}";
				}
			}
			return type.Name!;
		}

		string? GetTypeFromControlTheme()
		{
			if (state.GetParentTagName(1)?.Equals("ControlTheme") == true)
			{
				if (state.FindParentAttributeValue("TargetType", 1, 0) is string implicitSelectorTypeName)
				{
					return implicitSelectorTypeName;
				}
			}
			return null;
		}
	}

	public static bool ShouldTriggerCompletionListOn(char typedChar)
	{
		return char.IsLetterOrDigit(typedChar) || (typedChar == '/') || (typedChar == '<')
			|| (typedChar == ' ') || (typedChar == '.') || (typedChar == ':') || (typedChar == '$')
			|| (typedChar == '#') || (typedChar == '-') || (typedChar == '^') || (typedChar == '{')
			|| (typedChar == '=') || (typedChar == '[') || (typedChar == '|') || (typedChar == '(');
	}

	private int BuildCompletionsForMarkupExtension(MetadataProperty? property, List<Completion> completions, string fullText, XmlParser state, string data, string? currentAssemblyName)
	{
		int? forcedStart = null;
		var ext = MarkupExtensionParser.Parse(data);

		var transformedName = (ext.ElementName ?? "").Trim();
		if (Helper.LookupType(transformedName)?.IsMarkupExtension != true)
		{
			transformedName += "Extension";
		}

		if (ext.State == MarkupExtensionParser.ParserStateType.StartElement)
		{
			completions.AddRange(Helper.FilterTypeNames(ext.ElementName, markupExtensionsOnly: true)
				.Select(t => t.EndsWith("Extension") ? t.Substring(0, t.Length - "Extension".Length) : t)
				.Select(t => new Completion(t, CompletionKind.MarkupExtension)));
		}
		if ((ext.State == MarkupExtensionParser.ParserStateType.StartAttribute) ||
			(ext.State == MarkupExtensionParser.ParserStateType.InsideElement))
		{
			if (ext.State == MarkupExtensionParser.ParserStateType.InsideElement)
			{
				forcedStart = data.Length;
			}

			var isOnPlatform = ext.ElementName?.Trim().Equals("OnPlatform");
			var isOnFormFactor = ext.ElementName?.Trim().Equals("OnFormFactor");

			if ((isOnPlatform == true) || (isOnFormFactor == true))
			{
				// If we type a comma after a previous attribute: // {Binding Path=MyProp,
				// the parser shows that as InsideElement, though we really want that to
				// be StartAttribute for a list of completions relevant to the markup extension
				// i.e., above we'd get the completion list for {Binding} again
				var isActuallyStartAttribute = false;
				for (var i = data.Length - 1; i >= 0; i--)
				{
					if (data[i] == ',')
					{
						isActuallyStartAttribute = true;
						break;
					}
					if (data[i] == '=')
					{
						break;
					}
				}

				if (isActuallyStartAttribute || (ext.State == MarkupExtensionParser.ParserStateType.StartAttribute))
				{
					if (isOnPlatform == true)
					{
						completions.Add(new Completion("Windows", "Windows=", "Windows", CompletionKind.Enum));
						completions.Add(new Completion("macOS", "macOS=", "macOS", CompletionKind.Enum));
						completions.Add(new Completion("Linux", "Linux=", "Linux", CompletionKind.Enum));
						completions.Add(new Completion("Android", "Android=", "Android", CompletionKind.Enum));
						completions.Add(new Completion("iOS", "iOS=", "iOS", CompletionKind.Enum));
						completions.Add(new Completion("Browser", "Browser=", "Browser", CompletionKind.Enum));
					}
					else
					{
						completions.Add(new Completion("Desktop", "Desktop=", "Desktop", CompletionKind.Enum));
						completions.Add(new Completion("Mobile", "Mobile=", "Mobile", CompletionKind.Enum));
					}
				}
				else
				{
					var prop = Helper.LookupProperty(state?.TagName, state?.AttributeName);
					if (prop?.Type?.HasHintValues == true)
					{
						completions.AddRange(GetHintCompletions(prop.Type, null, currentAssemblyName));
					}
				}

				return forcedStart ?? ext.CurrentValueStart;
			}

			completions.AddRange(Helper.FilterPropertyNames(transformedName, ext.AttributeName ?? "", false, true)
				.Select(x => new Completion(x, x + "=", x, CompletionKind.Property)));

			var attribName = ext.AttributeName ?? "";
			var t = Helper.LookupType(transformedName);

			var ctorArgument = ext.AttributesCount == 0;
			//skip ctor hints when some property is already set
			if ((t != null) && t.IsMarkupExtension && (t.SupportCtorArgument != MetadataTypeCtorArgument.None) && ctorArgument)
			{
				if (t.SupportCtorArgument == MetadataTypeCtorArgument.HintValues)
				{
					if (t.HasHintValues)
					{
						// Pass fullText so Static/DynamicResource can include local x:Key values.
						completions.AddRange(GetHintCompletions(t, attribName, currentAssemblyName, fullText, state));
					}
				}
				else if (attribName.Contains('.'))
				{
					if (t.SupportCtorArgument != MetadataTypeCtorArgument.Type)
					{
						var split = attribName.Split('.');
						var type = split[0];
						var prop = split[1];

						var mType = Helper.LookupType(type);
						if ((mType != null) && (t.SupportCtorArgument == MetadataTypeCtorArgument.HintValues))
						{
							var hints = FilterHintValues(mType, prop, currentAssemblyName, state);
							completions.AddRange(hints.Select(x => new Completion(x, $"{type}.{x}", x, GetCompletionKindForHintValues(mType))));
						}

						var props = Helper.FilterPropertyNames(type, prop, false, false, true);
						completions.AddRange(props.Select(x => new Completion(x, $"{type}.{x}", x, CompletionKind.StaticProperty)));
					}
				}
				else
				{
					var types = Helper.FilterTypeNames(attribName,
						staticGettersOnly: t.SupportCtorArgument == MetadataTypeCtorArgument.Object);

					completions.AddRange(types.Select(x => new Completion(x, x, x, CompletionKind.Class)));

					if (property?.Type?.HasHintValues == true)
					{
						completions.Add(new Completion(property.Type.Name, property.Type.Name + ".", property.Type.Name, CompletionKind.Class));
					}
				}
			}
			else
			{
				var defaultProp = t?.Properties.FirstOrDefault(p => string.IsNullOrEmpty(p.Name));
				if (defaultProp?.Type?.HasHintValues ?? false)
				{
					completions.AddRange(GetHintCompletions(defaultProp.Type, ext.AttributeName ?? "", currentAssemblyName, fullText, state));
				}
			}
		}
		if ((ext.State == MarkupExtensionParser.ParserStateType.AttributeValue)
			|| (ext.State == MarkupExtensionParser.ParserStateType.BeforeAttributeValue))
		{
			var elementName = ext.ElementName?.Trim();
			MetadataProperty? prop;
			if ((elementName?.Equals("OnPlatform") == true) ||
				(elementName?.Equals("OnFormFactor") == true))
			{
				prop = Helper.LookupProperty(state.TagName, state.AttributeName);
			}
			else
			{
				prop = Helper.LookupProperty(transformedName, ext.AttributeName);
			}

			if (prop?.Type?.HasHintValues == true)
			{
				var start = data.Substring(ext.CurrentValueStart);
				completions.AddRange(GetHintCompletions(prop.Type, start, currentAssemblyName, fullText, state));
			}
			else
			{
				var resourceExt = Helper.LookupType(transformedName);
				if (resourceExt is not null &&
					IsResourceKeyMarkupExtension(resourceExt) &&
					(string.IsNullOrEmpty(ext.AttributeName) ||
						string.Equals(ext.AttributeName, "ResourceKey", StringComparison.OrdinalIgnoreCase)))
				{
					// ResourceKey= named property — same key catalog as ctor arg.
					var start = data.Substring(ext.CurrentValueStart);
					completions.AddRange(GetHintCompletions(resourceExt, start, currentAssemblyName, fullText, state));
				}
			}
		}

		return forcedStart ?? ext.CurrentValueStart;
	}

	private IEnumerable<Completion> FilterHintValuesForBindingPath(MetadataType bindingPathType, string? entered, string? currentAssemblyName, string? fullText, XmlParser state)
	{
		IEnumerable<Completion> ForPropertiesFromType(MetadataType? filterType, string? filter, Func<string, string>? fmtInsertText = null)
		{
			if (filterType != null)
			{
				foreach (var propertyName in MetadataHelper.FilterPropertyNames(filterType, filter, false, false))
				{
					yield return new Completion(propertyName, fmtInsertText?.Invoke(propertyName) ?? propertyName, propertyName, CompletionKind.DataProperty, Priority: 254);
				}
			}
		}

		IEnumerable<Completion> ForProperties(string? filterType, string? filter, Func<string, string>? fmtInsertText = null)
		{
			return ForPropertiesFromType(Helper.LookupType(filterType ?? ""), filter, fmtInsertText);
		}

		if (string.IsNullOrEmpty(entered))
		{
			return ForProperties(state.FindParentAttributeValue("(x\\:)?DataType"), entered);
		}

		var values = entered.Split('.');

		if (values.Length == 1)
		{
			if (values[0].StartsWith("$parent["))
			{
				return Helper.FilterTypes(entered.Substring("$parent[".Length))
					.Select(v => new Completion(v.Key, $"$parent[{v.Key}].", v.Key, CompletionKind.Class));
			}
			if (values[0].StartsWith("#"))
			{
				if (fullText is not null)
				{
					var nameMatch = MetadataHelper.FindElementByNameRegex.Matches(fullText);
					if (nameMatch is { Count: > 0 })
					{
						var result = new List<Completion>();
						foreach (Match m in nameMatch)
						{
							if (m.Success)
							{
								var name = m.Groups["AttribValue"].Value;
								result.Add(new Completion(name, $"#{name}", name, CompletionKind.Class));
							}
						}
						return result;
					}
				}

				return [];
			}

			return ForProperties(state.FindParentAttributeValue("(x\\:)?DataType"), entered);
		}

		var type = values[0];

		int i;

		if (values[0].StartsWith("$"))
		{
			i = 1;
			type = "Control";
			if (values[0] == "$self") //current control type
			{
				type = state.GetParentTagName(0);
			}
			else if (values[0] == "$parent") //parent control in the xaml
			{
				type = state.GetParentTagName(1) ?? "Control";
			}
			else if (values[0].StartsWith("$parent[")) //extract parent type
			{
				type = values[0].Substring("$parent[".Length, values[0].Length - "$parent[".Length - 1);
			}
		}
		else if (values[0].StartsWith("#"))
		{
			i = 1;
			//todo: find the control type etc ???
			type = "Control";
		}
		else
		{
			i = 0;
			type = state.FindParentAttributeValue("(x\\:)?DataType");
		}

		var mdType = Helper.LookupType(type ?? "");

		while ((mdType != null) && (i < (values.Length - 1)) && !string.IsNullOrEmpty(values[i]))
		{
			if ((i <= 1) && (values[i] == "DataContext"))
			{
				//assume parent.datacontext is x:datatype so we have some intellisense
				type = state.FindParentAttributeValue("(x\\:)?DataType");
				mdType = type is not null ? Helper.LookupType(type) : null;
			}
			else
			{
				mdType = mdType.Properties.FirstOrDefault(p => p.Name == values[i])?.Type;
				type = mdType?.FullName;
			}
			i++;
		}

		return ForPropertiesFromType(mdType, values[i], p => $"{string.Join(".", values.Take(i).ToArray())}.{p}");
	}

	private static int GetCompletionPriority(CompletionKind kind)
	{
		return kind switch
		{
			CompletionKind.MarkupExtension => 0,
			CompletionKind.Namespace => 1,
			CompletionKind.Property => 2,
			CompletionKind.AttachedProperty => 3,
			CompletionKind.StaticProperty => 4,
			CompletionKind.Event => 5,
			CompletionKind.AttachedEvent => 6,
			CompletionKind.Class => 7,
			CompletionKind.Enum => 8,
			CompletionKind.None => 9,
			_ => (int) kind
		};
	}

	/// <summary>
	/// Types that are typically empty / leaf controls — complete as self-closing <c>Name /&gt;</c>.
	/// Everything else gets paired tags <c>Name&gt;&lt;/Name&gt;</c> with the caret in between.
	/// </summary>
	private static readonly HashSet<string> SelfClosingElementNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"TextBlock", "TextBox", "Image", "Separator", "ProgressBar", "Slider",
		"CheckBox", "RadioButton", "ToggleSwitch", "NumericUpDown", "AutoCompleteBox",
		"MaskedTextBox", "ButtonSpinner", "CalendarDatePicker", "DatePicker", "TimePicker",
		"Path", "Ellipse", "Rectangle", "Line", "Polyline", "Polygon", "Thumb", "ScrollBar",
		"TickBar", "GlyphRun", "DrawingImage", "CroppedBitmap", "WriteableBitmap"
	};

	private static ElementCompletionInfo GetElementCompletionInfo(string key,
		MetadataType? type)
	{
		var xamlName = key;
		var insertText = xamlName;
		var recommendedCursorOffset = default(int?);
		var triggerCompletionAfterInsert = false;
		if (type is not null)
		{
			if (type.IsMarkupExtension)
			{
				if (xamlName.EndsWith("extension", StringComparison.OrdinalIgnoreCase))
				{
					xamlName = xamlName.Substring(0, key.Length - 9 /* length of "extension" */);
				}
			}
			insertText = xamlName;
			if (type.IsGeneric)
			{
				var tArgsStart = xamlName.IndexOf('`');
				if (tArgsStart > -1)
				{
					var xamlNameBuilder = new StringBuilder();
					var insertTextBuilder = new StringBuilder();
					xamlNameBuilder.Append(xamlName, 0, tArgsStart);
					insertTextBuilder.Append(xamlName, 0, tArgsStart);
					var args = xamlName.Substring(tArgsStart + 1);
					if (int.TryParse(args
							, NumberStyles.Number
							, CultureInfo.InvariantCulture, out var nArgs))
					{
						if (nArgs == 1)
						{
							xamlNameBuilder.Append("<T>");
							insertTextBuilder.Append(" x:TypeArguments=\"\"");
							recommendedCursorOffset = insertTextBuilder.Length - 1;
						}
						else
						{
							xamlNameBuilder.Append('<');
							insertTextBuilder.Append(" x:TypeArguments=\"");
							recommendedCursorOffset = insertTextBuilder.Length - 1;
							for (var i = 0; i < nArgs; i++)
							{
								xamlNameBuilder.Append('T');
								xamlNameBuilder.Append(i + 1);
								xamlNameBuilder.Append(',');
								insertTextBuilder.Append(',');
							}
							xamlNameBuilder[xamlNameBuilder.Length - 1] = '>';
							insertTextBuilder[insertTextBuilder.Length - 1] = '"';
						}
						xamlName = xamlNameBuilder.ToString();
						insertText = insertTextBuilder.ToString();
						triggerCompletionAfterInsert = true;
					}
				}
			}
			else if (!type.IsMarkupExtension)
			{
				// User already typed '<' — InsertText is the rest of the tag only.
				(insertText, recommendedCursorOffset) = BuildElementTagInsert(xamlName);
			}
		}
		else
		{
			(insertText, recommendedCursorOffset) = BuildElementTagInsert(xamlName);
		}

		return new(xamlName, insertText, null, recommendedCursorOffset, triggerCompletionAfterInsert);
	}

	/// <summary>
	/// Leaf controls → <c>TextBlock /&gt;</c> (caret after name).
	/// Containers → <c>StackPanel&gt;&lt;/StackPanel&gt;</c> (caret between tags).
	/// </summary>
	public static (string InsertText, int RecommendedCursorOffset) BuildElementTagInsert(string xamlName)
	{
		if (PreferSelfClosingElement(xamlName))
		{
			// <TextB → TextBlock| />
			return (xamlName + " />", xamlName.Length);
		}

		// <Stack → StackPanel>|</StackPanel>
		var insert = xamlName + "></" + xamlName + ">";
		return (insert, xamlName.Length + 1);
	}

	/// <summary>
	/// True for known leaf/empty controls. Unknown and container-like types use paired tags.
	/// </summary>
	internal static bool PreferSelfClosingElement(string xamlName)
	{
		if (string.IsNullOrEmpty(xamlName))
		{
			return false;
		}

		var shortName = xamlName;
		var colon = xamlName.LastIndexOf(':');
		if (colon >= 0 && colon < xamlName.Length - 1)
		{
			shortName = xamlName.Substring(colon + 1);
		}

		// *Panel, Grid, Border, etc. always take children.
		if (shortName.EndsWith("Panel", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Grid", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Border", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Canvas", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("DockPanel", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("ScrollViewer", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Viewbox", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("ContentControl", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("UserControl", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Window", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("ItemsControl", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("ListBox", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("ComboBox", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("TreeView", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("TabControl", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Menu", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("MenuItem", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("ToolBar", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Button", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("ToggleButton", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("RepeatButton", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("SplitView", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Expander", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Flyout", StringComparison.OrdinalIgnoreCase) ||
			shortName.Equals("Popup", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return SelfClosingElementNames.Contains(shortName);
	}

	/// <summary>
	/// Pin the line's leading whitespace to <paramref name="originalLine"/>.
	/// Used after Enter/Tab completion commit when the XML editor smart-indents the line.
	/// </summary>
	public static string PreserveLineLeadingWhitespace(string originalLine, string newLine)
	{
		if (originalLine is null || newLine is null)
		{
			return newLine;
		}

		var oldWs = GetLeadingWhitespace(originalLine);
		var newWs = GetLeadingWhitespace(newLine);
		// Always re-apply the original indent string; content after leading ws is kept from newLine.
		return oldWs + newLine.Substring(newWs.Length);
	}

	public static string GetLeadingWhitespace(string line)
	{
		if (string.IsNullOrEmpty(line))
		{
			return string.Empty;
		}

		var i = 0;
		while (i < line.Length && char.IsWhiteSpace(line[i]))
		{
			i++;
		}

		return line.Substring(0, i);
	}

	/// <summary>
	/// Clamps the completion replace span so typed filter text (e.g. <c>TextB</c>) is replaced
	/// by the insertion text. Testable without the VS editor.
	/// </summary>
	public static (int Start, int Length) GetApplicableSpan(int engineStartPosition, int caretPosition)
	{
		var start = engineStartPosition;
		if (start < 0)
		{
			start = 0;
		}

		if (start > caretPosition)
		{
			start = caretPosition;
		}

		return (start, caretPosition - start);
	}

	/// <summary>
	/// Simulates VS commit: replace <c>[start, caret)</c> with <paramref name="insertText"/>.
	/// </summary>
	public static string ApplyCompletionReplace(string fullText, int start, int caret, string insertText)
	{
		if (fullText is null)
		{
			throw new ArgumentNullException(nameof(fullText));
		}

		if ((start < 0) || (caret < start) || (caret > fullText.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(caret));
		}

		return fullText.Substring(0, start) + insertText + fullText.Substring(caret);
	}

	/// <summary>
	/// Converts engine <see cref="Completion.RecommendedCursorOffset"/> (index into insert text)
	/// to VS-style offset from the end of the inserted text (how far left to move the caret).
	/// </summary>
	public static int GetCursorOffsetFromEnd(string insertText, int? recommendedCursorOffset)
	{
		if (string.IsNullOrEmpty(insertText) || !recommendedCursorOffset.HasValue)
		{
			return 0;
		}

		return insertText.Length - recommendedCursorOffset.Value;
	}

	private List<Completion> GetHintCompletions(MetadataType type, string? entered, string? currentAssemblyName = null, string? fullText = null, XmlParser? state = null)
	{
		var kind = GetCompletionKindForHintValues(type);

		var completions = FilterHintValues(type, entered, currentAssemblyName, state)
			.Select(val => new Completion(val, kind)).ToList();

		// Local document x:Key values for StaticResource / DynamicResource (theme keys stay in HintValues).
		if (IsResourceKeyMarkupExtension(type) && !string.IsNullOrEmpty(fullText))
		{
			var existing = new HashSet<string>(completions.Select(c => c.InsertText), StringComparer.Ordinal);
			foreach (var key in ResourceKeyScanner.FindKeys(fullText))
			{
				if (existing.Add(key))
				{
					completions.Add(new Completion(key, kind));
				}
			}
		}

		if ((type.FullName == "{BindingPath}") && (state != null))
		{
			completions.AddRange(FilterHintValuesForBindingPath(type, entered, currentAssemblyName, fullText, state));
		}
		return completions;
	}

	private static bool IsResourceKeyMarkupExtension(MetadataType type)
	{
		var name = type.FullName ?? type.Name ?? "";
		return name.EndsWith("StaticResourceExtension", StringComparison.Ordinal) ||
			name.EndsWith("DynamicResourceExtension", StringComparison.Ordinal) ||
			name.Equals("StaticResource", StringComparison.Ordinal) ||
			name.Equals("DynamicResource", StringComparison.Ordinal);
	}

	private string GetXmlnsFullName(MetadataType type, char namespaceSeparator = '|')
	{
		if ((Helper.Metadata?.InverseNamespace.TryGetValue(type.FullName, out var ns) == true)
			&& !string.IsNullOrEmpty(ns))
		{
			var alias = Helper.Aliases?.FirstOrDefault(a => Equals(a.Value, ns));
			if (alias is not null && !string.IsNullOrEmpty(alias.Value.Key))
			{
				return $"{alias.Value.Key}{namespaceSeparator}{type.Name}";
			}
		}
		return type.Name!;
	}

	private void ProcessStyleSetter(string setterPropertyName, XmlParser state, List<Completion> completions, string? currentAssemblyName)
	{
		const string selectorTypes = @"(?<type>([\w|])+)|([:\.#/]\w+)";

		// TODO: This improves ControlThemes to properly suggest properties in Setters,
		// but we still need to improve this for nested Styles:
		// <Style Selector="^:pointerover">
		// Won't show suggestions (or incorrect ones), because the else clause below will fail
		// to find a type in that selector and we don't search up the Xml tree
		string? selectorTypeName = null;
		if (state.GetParentTagName(1)?.Equals("ControlTheme") == true)
		{
			selectorTypeName = state.FindParentAttributeValue("TargetType", 1, 0);
		}
		else
		{
			if (state.FindParentAttributeValue("Selector", 1, 0)?.Trim() is { Length: > 0 } selector)
			{
				if (selector[0] == '^')
				{
					selectorTypeName = state.FindParentAttributeValue("TargetType", 2, 0);
				}
				else
				{
					var matches = Regex.Matches(selector, selectorTypes);
					var types = matches.OfType<Match>().Select(m => m.Groups["type"].Value).Where(v => !string.IsNullOrEmpty(v));
					selectorTypeName = types.LastOrDefault()?.Replace('|', ':') ?? "Control";
				}
			}
		}

		if (string.IsNullOrEmpty(selectorTypeName))
		{
			return;
		}

		if (setterPropertyName == "Property")
		{
			var value = state.AttributeValue ?? "";

			if (value.Contains('.'))
			{
				var curStart = state.CurrentValueStart ?? 0;
				var dotPos = value.IndexOf(".");
				var typeName = value.Substring(0, dotPos);
				var compName = value.Substring(dotPos + 1);
				curStart = curStart + dotPos + 1;

				var sameType = state.GetParentTagName(1) == typeName;

				completions.AddRange(Helper.FilterPropertyNames(typeName, compName, true, true)
					.Select(p => new Completion(p, p, p, CompletionKind.AttachedProperty)));
			}
			else
			{
				completions.AddRange(Helper.FilterPropertyNames(selectorTypeName, value, false, true)
					.Select(x => new Completion(x, CompletionKind.Property)));

				completions.AddRange(Helper.FilterTypeNames(value, true).Select(x => new Completion(x, CompletionKind.Class)));
			}
		}
		else if (setterPropertyName == "Value")
		{
			var setterProperty = state.FindParentAttributeValue("Property", maxLevels: 0);
			if (setterProperty is not null)
			{
				if (setterProperty.Contains('.'))
				{
					var vals = setterProperty.Split('.');
					selectorTypeName = vals[0];
					setterProperty = vals[1];
				}

				var setterProp = Helper.LookupProperty(selectorTypeName, setterProperty);
				if ((setterProp?.Type?.HasHintValues == true) && state.AttributeValue is not null)
				{
					completions.AddRange(GetHintCompletions(setterProp.Type, state.AttributeValue, currentAssemblyName));
				}
			}
		}
	}

	private static List<Completion> SortCompletions(List<Completion> completions)
	{
		// Group the completions based on Kind, and sort the completions for each group
		return completions
			.GroupBy(i => i.Kind, (kind, compl) =>
				(Kind: kind, Completions: compl
					.OrderBy(j => j.Priority).ThenBy(j => j.DisplayText)))
			.OrderBy(i => GetCompletionPriority(i.Kind))
			.SelectMany(i => i.Completions)
			.ToList();
	}

	#endregion

	#region Classes

	public class MetadataHelper
	{
		#region Fields

		private string? _currentAssemblyName;
		private static Regex? _findElementByNameRegex;

		private Dictionary<string, MetadataType>? _types;

		#endregion

		#region Properties

		public Dictionary<string, string>? Aliases { get; private set; }
		public Metadata? Metadata { get; private set; }

		internal static Regex FindElementByNameRegex =>
			_findElementByNameRegex ??=
				new("\\s(?:(x\\:)?Name)=\"(?<AttribValue>[\\w\\:\\s\\|\\.]+)\"", RegexOptions.Compiled);

		#endregion

		#region Methods

		public IEnumerable<string> FilterEventNames(string typeName, string? propName,
			bool attached)
		{
			var t = LookupType(typeName);
			propName ??= "";
			if (t == null)
			{
				return [];
			}

			return t.Events.Where(n => (n.IsAttached == attached) && n.Name.StartsWith(propName, StringComparison.OrdinalIgnoreCase)).Select(n => n.Name);
		}

		public static IEnumerable<MetadataProperty> FilterProperty(MetadataType? t, string? propName,
			bool? attached,
			bool hasSetter,
			bool staticGetter = false
		)
		{
			propName ??= "";
			if (t == null)
			{
				return [];
			}

			var e = t.Properties.Where(p => p.Name.StartsWith(propName, StringComparison.OrdinalIgnoreCase) && (hasSetter ? p.HasSetter : p.HasGetter));

			if (attached.HasValue)
			{
				e = e.Where(p => p.IsAttached == attached);
			}
			if (staticGetter)
			{
				e = e.Where(p => p.IsStatic && p.HasGetter);
			}
			else
			{
				e = e.Where(p => !p.IsStatic);
			}

			return e;
		}

		public IEnumerable<string> FilterPropertyNames(string typeName, string? propName,
			bool? attached,
			bool hasSetter,
			bool staticGetter = false)
		{
			var t = LookupType(typeName);
			return FilterPropertyNames(t, propName, attached, hasSetter, staticGetter);
		}

		public static IEnumerable<string> FilterPropertyNames(MetadataType? t,
			string? propName,
			bool? attached,
			bool hasSetter,
			bool staticGetter = false)
		{
			return FilterProperty(t, propName, attached, hasSetter, staticGetter).Select(p => p.Name);
		}

		public IEnumerable<string> FilterTypeNames(string? prefix, bool withAttachedPropertiesOrEventsOnly = false, bool markupExtensionsOnly = false, bool staticGettersOnly = false, bool xamlDirectiveOnly = false)
		{
			return FilterTypes(prefix, withAttachedPropertiesOrEventsOnly, markupExtensionsOnly, staticGettersOnly, xamlDirectiveOnly).Select(s => s.Key);
		}

		public IEnumerable<KeyValuePair<string, MetadataType>> FilterTypes(string? prefix, bool withAttachedPropertiesOrEventsOnly = false, bool markupExtensionsOnly = false, bool staticGettersOnly = false, bool xamlDirectiveOnly = false)
		{
			if (_types is null)
			{
				return [];
			}

			prefix ??= "";

			var e = _types
				.Where(t => (t.Value.IsXamlDirective == xamlDirectiveOnly) && t.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				.Where(x => !x.Key.Equals("ControlTemplateResult") && !x.Key.Equals("DataTemplateExtensions"));
			if (withAttachedPropertiesOrEventsOnly)
			{
				e = e.Where(t => t.Value.HasAttachedProperties || t.Value.HasAttachedEvents);
			}
			if (markupExtensionsOnly)
			{
				e = e.Where(t => t.Value.IsMarkupExtension);
			}
			if (staticGettersOnly)
			{
				e = e.Where(t => t.Value.HasStaticGetProperties);
			}

			return e;
		}

		public MetadataProperty? LookupProperty(string? typeName, string? propName)
		{
			return LookupType(typeName)?.Properties?.FirstOrDefault(p => p.Name == propName);
		}

		public MetadataType? LookupType(string? name)
		{
			if (name is null)
			{
				return null;
			}

			MetadataType? rv = null;
			if (!(_types?.TryGetValue(name, out rv) == true))
			{
				// Markup extensions used as XML elements will fail to lookup because
				// the tag name won't include 'Extension'
				_types?.TryGetValue($"{name}Extension", out rv);
			}
			return rv;
		}

		public void SetMetadata(Metadata metadata, string xml, string? currentAssemblyName = null)
		{
			var aliases = GetNamespaceAliases(xml);

			//Check if metadata and aliases can be reused
			if ((Metadata == metadata) && (Aliases != null) && (_types != null) && (currentAssemblyName == _currentAssemblyName))
			{
				if (aliases.Count == Aliases.Count)
				{
					var mismatch = false;
					foreach (var alias in aliases)
					{
						if (!Aliases.ContainsKey(alias.Key) || (Aliases[alias.Key] != alias.Value))
						{
							mismatch = true;
							break;
						}
					}

					if (!mismatch)
					{
						return;
					}
				}
			}
			Aliases = aliases;
			Metadata = metadata;
			_types = null;
			_currentAssemblyName = currentAssemblyName;

			var types = new Dictionary<string, MetadataType>();
			foreach (var alias in Aliases.Concat([new KeyValuePair<string, string>("", "")]))
			{
				var aliasValue = alias.Value ?? "";

				if (!string.IsNullOrEmpty(_currentAssemblyName) && aliasValue.StartsWith("clr-namespace:") && !aliasValue.Contains(";assembly="))
				{
					aliasValue = $"{aliasValue};assembly={_currentAssemblyName}";
				}

				if (!metadata.Namespaces.TryGetValue(aliasValue, out var ns))
				{
					continue;
				}

				var prefix = alias.Key.Length == 0 ? "" : alias.Key + ":";
				foreach (var type in ns.Values)
				{
					types[prefix + type.Name] = type;
				}
			}

			_types = types;
		}

		#endregion
	}

	#endregion

	#region Records

	private record struct ElementCompletionInfo(string DisplayText, string InsertText, string? Suffix, int? RecommendedCursorOffset, bool TriggerCompletionAfterInsert);

	#endregion
}