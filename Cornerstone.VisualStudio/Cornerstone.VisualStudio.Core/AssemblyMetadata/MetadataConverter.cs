#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

#endregion

namespace Cornerstone.VisualStudio.Core.AssemblyMetadata;

internal record class AvaloniaResourcesIndexEntry(string? Path, int Offset, int Size);

public static class MetadataConverter
{
	#region Constants

	private const int CurrentResourceIndex = 2;
	private const int LegacyXmlResourceIndex = 1;

	#endregion

	#region Fields

	private static readonly string[] _avaloniaBaseType =
	[
		"Avalonia.Markup.Xaml.MarkupExtensions.BindingExtension,",
		"Avalonia.Data.Binding,",
		"Avalonia.Controls.Control,",
		"Avalonia.Data.TemplateBinding,",
		"Portable.Xaml.Markup.TypeExtension,",
		"Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension,",
		"Avalonia.Markup.Xaml.MarkupExtensions.StaticResourceExtension,",
		"Avalonia.Media.Brushes",
		"Avalonia.Styling.Selector,",
		"Avalonia.Media.Imaging.IBitmap",
		"Avalonia.Media.IImage",
		"Avalonia.Controls.WindowIcon,",
		"Avalonia.Markup.Xaml.Styling.StyleIncludeExtension,",
		"Avalonia.Markup.Xaml.Styling.StyleInclude,",
		"Avalonia.Markup.Xaml.Styling.StyleIncludeExtension,"
	];

	private static readonly Regex _extractType = new(
		"System.Nullable`1<(?<Type>.*)>|System.Nullable`1\\[\\[(?<Typ" +
		"e>.*)]].*",
		RegexOptions.CultureInvariant
		| RegexOptions.Compiled
	);

	#endregion

	#region Methods

	public static Metadata ConvertMetadata(IMetadataReaderSession provider)
	{
		var types = new Dictionary<string, MetadataType>();
		var typeDefs = new Dictionary<MetadataType, ITypeInformation>();
		var metadata = new Metadata();
		var resourceUrls = new List<string>();
		var avaresValues = new List<AvaresInfo>();
		var pseudoclasses = new HashSet<string>();
		var typepseudoclasses = new HashSet<string>();

		var ignoredResExt = new[] { ".resources", ".rd.xml", "!AvaloniaResources" };

		bool SkipRes(string res)
		{
			return ignoredResExt.Any(r => res.EndsWith(r, StringComparison.OrdinalIgnoreCase));
		}

		PreProcessTypes(types, metadata);
		var targetAssembly = provider.Assemblies.FirstOrDefault() ?? throw new InvalidOperationException("IMetadataReaderSession.Assemblies list is empty.");
		foreach (var asm in provider.Assemblies)
		{
			var aliases = new Dictionary<string, string[]>();

			ProcessWellKnownAliases(asm, aliases);
			ProcessCustomAttributes(asm, aliases);

			Func<ITypeInformation, bool> typeFilter = type => !type.IsInterface && type.IsPublic;

			if ((asm.AssemblyName == provider.TargetAssemblyName)
				||
				asm.InternalsVisibleTo.Any(att =>
				{
					var endNameIndex = att.IndexOf(',');
					var assemblyName = att;
					var targetPublicKey = targetAssembly.PublicKey;
					if (endNameIndex > 0)
					{
						assemblyName = att.Substring(0, endNameIndex);
					}
					if (assemblyName == targetAssembly.Name)
					{
						if (endNameIndex == -1)
						{
							return true;
						}
						var publicKeyIndex = att.IndexOf("PublicKey", endNameIndex, StringComparison.OrdinalIgnoreCase);
						if (publicKeyIndex > 0)
						{
							publicKeyIndex += 9;
							if (publicKeyIndex > att.Length)
							{
								return false;
							}
							while ((publicKeyIndex < att.Length) && att[publicKeyIndex] is ' ' or '=')
							{
								publicKeyIndex++;
							}
							if (targetPublicKey.Length == (att.Length - publicKeyIndex))
							{
								for (var i = publicKeyIndex; i < att.Length; i++)
								{
									if (att[i] != targetPublicKey[i - publicKeyIndex])
									{
										return false;
									}
								}
								return true;
							}
						}
					}
					return false;
				}))
			{
				typeFilter = type => (type.Name != "<Module>") && !type.IsInterface && !type.IsAbstract;
			}

			var asmTypes = asm.Types.Where(typeFilter).ToArray();

			foreach (var type in asmTypes)
			{
				var mt = types[type.AssemblyQualifiedName] = ConvertTypeInformation(type);
				typeDefs[mt] = type;
				metadata.AddType("clr-namespace:" + type.Namespace + ";assembly=" + asm.Name, mt);
				var usingNamespace = $"using:{type.Namespace}";
				if (!aliases.TryGetValue(type.Namespace, out var nsAliases))
				{
					nsAliases = [usingNamespace];
					aliases[type.Namespace] = nsAliases;
				}
				else if (!nsAliases.Contains(usingNamespace))
				{
					aliases[type.Namespace] = nsAliases.Union([usingNamespace]).ToArray();
				}

				foreach (var alias in nsAliases)
				{
					metadata.AddType(alias, mt);
				}
			}

			ProcessAvaloniaResources(asm, asmTypes, avaresValues);

			resourceUrls.AddRange(asm.ManifestResourceNames.Where(r => !SkipRes(r)).Select(r => $"resm:{r}?assembly={asm.Name}"));
		}

		var at = types.Values.ToArray();
		foreach (var type in at)
		{
			typeDefs.TryGetValue(type, out var typeDef);

			var ctors = typeDef?.Methods
				.Where(m => m.IsPublic && !m.IsStatic && (m.Name == ".ctor") && (m.Parameters.Count == 1));

			if (typeDef?.IsEnum ?? false)
			{
				foreach (var value in typeDef.EnumValues)
				{
					var p = new MetadataProperty(value, type, type, false, true, true, false);

					type.Properties.Add(p);
				}
			}

			var level = 0;
			typepseudoclasses.Clear();

			type.TemplateParts = (typeDef?.TemplateParts ??
					[])
				.Select(item => (Type: ConvertTypeInformation(item.Type), item.Name))
				.ToList();

			while (typeDef != null)
			{
				foreach (var pc in typeDef.Pseudoclasses)
				{
					typepseudoclasses.Add(pc);
					pseudoclasses.Add(pc);
				}

				var currentType = types.GetValueOrDefault(typeDef.AssemblyQualifiedName);
				foreach (var prop in typeDef.Properties)
				{
					if (!prop.IsVisibleTo(targetAssembly))
					{
						continue;
					}

					var propertyType = GetType(types, prop.TypeFullName, prop.QualifiedTypeFullName);

					var p = new MetadataProperty(prop.Name, propertyType,
						currentType, false, prop.IsStatic, prop.HasPublicGetter,
						prop.HasPublicSetter);

					type.Properties.Add(p);
				}

				foreach (var eventDef in typeDef.Events)
				{
					var e = new MetadataEvent(eventDef.Name, GetType(types, eventDef.TypeFullName, eventDef.QualifiedTypeFullName),
						types.GetValueOrDefault(typeDef.FullName, typeDef.AssemblyQualifiedName), false);

					type.Events.Add(e);
				}

				if (level == 0)
				{
					foreach (var fieldDef in typeDef.Fields)
					{
						if (fieldDef.IsStatic && fieldDef.IsPublic)
						{
							if (fieldDef.IsRoutedEvent || fieldDef.Name.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
							{
								var name = fieldDef.Name;
								if (fieldDef.Name.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
								{
									name = name.Substring(0, name.Length - "Event".Length);
								}

								type.Events.Add(new MetadataEvent(name,
									types.GetValueOrDefault(fieldDef.ReturnTypeFullName, fieldDef.QualifiedTypeFullName),
									types.GetValueOrDefault(typeDef.FullName, typeDef.AssemblyQualifiedName),
									true));
							}
							else if (fieldDef.Name.EndsWith("Property", StringComparison.OrdinalIgnoreCase)
									&& fieldDef.ReturnTypeFullName.StartsWith("Avalonia.AttachedProperty`1")
									)
							{
								var name = fieldDef.Name.Substring(0, fieldDef.Name.Length - "Property".Length);

								IMethodInformation? setMethod = null;
								IMethodInformation? getMethod = null;

								var setMethodName = $"Set{name}";
								var getMethodName = $"Get{name}";

								foreach (var methodDef in typeDef.Methods)
								{
									if (methodDef.Name.Equals(setMethodName, StringComparison.OrdinalIgnoreCase) && methodDef.IsStatic && methodDef.IsPublic
										&& (methodDef.Parameters.Count == 2))
									{
										setMethod = methodDef;
									}
									if (methodDef.IsStatic
										&& methodDef.Name.Equals(getMethodName, StringComparison.OrdinalIgnoreCase)
										&& methodDef.IsPublic
										&& (methodDef.Parameters.Count == 1)
										&& !string.IsNullOrEmpty(methodDef.ReturnTypeFullName)
										)
									{
										getMethod = methodDef;
									}
								}

								if (getMethod is not null)
								{
									type.Properties.Add(new MetadataProperty(name,
										types.GetValueOrDefault(getMethod.ReturnTypeFullName, getMethod.QualifiedReturnTypeFullName),
										types.GetValueOrDefault(typeDef.FullName, typeDef.AssemblyQualifiedName),
										true,
										false,
										true,
										setMethod is not null));
								}
							}
							else if (type.IsStatic)
							{
								type.Properties.Add(new MetadataProperty(fieldDef.Name, null, type, false, true, true, false));
							}
						}
					}
				}

				if (typeDef.FullName == "Avalonia.AvaloniaObject")
				{
					type.IsAvaloniaObjectType = true;
				}

				typeDef = typeDef.GetBaseType();
				level++;
			}

			type.HasAttachedProperties = type.Properties.Any(p => p.IsAttached);
			type.HasAttachedEvents = type.Events.Any(e => e.IsAttached);
			type.HasStaticGetProperties = type.Properties.Any(p => p.IsStatic && p.HasGetter);
			type.HasSetProperties = type.Properties.Any(p => !p.IsStatic && p.HasSetter);
			if (typepseudoclasses.Count > 0)
			{
				type.HasPseudoClasses = true;
				type.PseudoClasses = typepseudoclasses.ToArray();
			}

			if (ctors?.Any() == true)
			{
				var supportType = ctors.Any(m => m.Parameters[0].TypeFullName == "System.Type");
				var supportObject = ctors.Any(m => (m.Parameters[0].TypeFullName == "System.Object") ||
					(m.Parameters[0].TypeFullName == "System.String"));

				if ((types.TryGetValue(ctors.First().Parameters[0].QualifiedTypeFullName, out var parType)
						|| types.TryGetValue(ctors.First().Parameters[0].QualifiedTypeFullName, out parType))
					&& parType.HasHintValues)
				{
					type.SupportCtorArgument = MetadataTypeCtorArgument.HintValues;
					type.HasHintValues = true;
					type.HintValues = parType.HintValues;
				}
				else if (supportType && supportObject)
				{
					type.SupportCtorArgument = MetadataTypeCtorArgument.TypeAndObject;
				}
				else if (supportType)
				{
					type.SupportCtorArgument = MetadataTypeCtorArgument.Type;
				}
				else if (supportObject)
				{
					type.SupportCtorArgument = MetadataTypeCtorArgument.Object;
				}
			}
		}

		PostProcessTypes(types, metadata, resourceUrls, avaresValues, pseudoclasses);

		MetadataType? GetType(Dictionary<string, MetadataType> types, params string[] keys)
		{
			MetadataType? type = null;
			foreach (var key in keys)
			{
				if (types.TryGetValue(key, out type))
				{
					break;
				}
				if (key.StartsWith("System.Nullable`1", StringComparison.OrdinalIgnoreCase))
				{
					var typeName = _extractType.Match(key);
					if (typeName.Success && types.TryGetValue(typeName.Groups[1].Value, out type))
					{
						type = new MetadataType(key)
						{
							AssemblyQualifiedName = type.AssemblyQualifiedName,
							FullName = $"System.Nullable`1<{type.FullName}>",
							IsNullable = true,
							UnderlyingType = type
						};
						types.Add(key, type);
						break;
					}
				}
			}
			return type;
		}

		return metadata;
	}

	public static MetadataType ConvertTypeInformation(ITypeInformation type)
	{
		var mt = new MetadataType(type.Name)
		{
			FullName = type.FullName,
			AssemblyQualifiedName = type.AssemblyQualifiedName,
			IsStatic = type.IsStatic,
			IsMarkupExtension = IsMarkupExtension(type),
			IsEnum = type.IsEnum,
			HasHintValues = type.IsEnum,
			IsGeneric = type.IsGeneric,
			IsAbstract = type.IsAbstract
		};
		if (mt.IsEnum)
		{
			mt.HintValues = type.EnumValues.ToArray();
		}
		return mt;
	}

	internal static bool IsMarkupExtension(ITypeInformation type)
	{
		var def = type;

		while (def != null)
		{
			if (def.Name == "MarkupExtension")
			{
				return true;
			}
			def = def.GetBaseType();
		}

		//in avalonia 0.9 there is no required base class, but convention only
		if (type.FullName.EndsWith("Extension") && type.Methods.Any(m => m.Name == "ProvideValue"))
		{
			return true;
		}
		if (type.Name.Equals("OnPlatformExtension") || type.Name.Equals("OnFormFactorExtension"))
		{
			// Special case for this, as it the type info can't find the ProvideValue method
			return true;
		}

		return false;
	}

	private static void PostProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata,
		IEnumerable<string> resourceUrls, List<AvaresInfo> avaResValues, HashSet<string> pseudoclasses)
	{
		bool Rhasext(string resource, string ext)
		{
			return resource.StartsWith("resm:") ? resource.Contains(ext + "?assembly=") : resource.EndsWith(ext);
		}

		var allresourceUrls = avaResValues.Select(v => v.GlobalUrl).Concat(resourceUrls).ToArray();

		var resType = new MetadataType("avares://,resm:")
		{
			IsStatic = true,
			HasHintValues = true,
			HintValues = allresourceUrls
		};

		types.Add(resType.Name, resType);

		var xamlResType = new MetadataType("avares://*.xaml,resm:*.xaml")
		{
			HasHintValues = true,
			HintValues = resType.HintValues.Where(r => Rhasext(r, ".xaml") || Rhasext(r, ".paml") || Rhasext(r, ".axaml")).ToArray()
		};

		var styleResType = new MetadataType("Style avares://*.xaml,resm:*.xaml")
		{
			HasHintValues = true,
			HintValues = avaResValues.Where(v => v.ReturnTypeFullName.StartsWith("Avalonia.Styling.Style"))
				.Select(v => v.GlobalUrl)
				.Concat(resourceUrls.Where(r => Rhasext(r, ".xaml") || Rhasext(r, ".paml") || Rhasext(r, ".axaml")))
				.ToArray()
		};

		types.Add(styleResType.Name, styleResType);

		IEnumerable<string> FilterLocalRes(MetadataType type, string? currentAssemblyName)
		{
			if (currentAssemblyName is not null)
			{
				var localResPrefix = $"avares://{currentAssemblyName}";
				var resmSuffix = $"?assembly={currentAssemblyName}";

				foreach (var hint in type.HintValues ?? [])
				{
					if (hint.StartsWith("avares://"))
					{
						if (hint.StartsWith(localResPrefix))
						{
							yield return hint.Substring(localResPrefix.Length);
						}
					}
					else if (hint.StartsWith("resm:"))
					{
						if (hint.EndsWith(resmSuffix))
						{
							yield return hint.Substring(0, hint.Length - resmSuffix.Length);
						}
					}
				}
			}
		}

		resType.XamlContextHintValuesFunc = (a, t, p) => FilterLocalRes(xamlResType, a);
		xamlResType.XamlContextHintValuesFunc = (a, t, p) => FilterLocalRes(xamlResType, a);
		styleResType.XamlContextHintValuesFunc = (a, t, p) => FilterLocalRes(styleResType, a);

		types.Add(xamlResType.Name, xamlResType);

		var allProps = new Dictionary<string, MetadataProperty>();

		foreach (var type in types.Where(t => t.Value.IsAvaloniaObjectType))
		{
			foreach (var v in type.Value.Properties.Where(p => p.HasSetter && p.HasGetter))
			{
				allProps[v.Name] = v;
			}
		}

		// Remmap avalonia base type
		var avaloniaBaseType = new Dictionary<string, MetadataType>(StringComparer.OrdinalIgnoreCase);

		foreach (var kv in types)
		{
			if (_avaloniaBaseType.FirstOrDefault((a, b) => b.StartsWith(a, StringComparison.OrdinalIgnoreCase), kv.Key) is string at)
			{
				var len = at.Length - 1;
				if (at[len] == ',')
				{
					avaloniaBaseType.Add(at.Substring(0, at.Length - 1), kv.Value);
				}
				else
				{
					avaloniaBaseType.Add(at, kv.Value);
				}
			}
		}

		var allAvaloniaProps = allProps.Keys.ToArray();

		if (!avaloniaBaseType.TryGetValue("Avalonia.Markup.Xaml.MarkupExtensions.BindingExtension", out var bindingExtType))
		{
			if (avaloniaBaseType.TryGetValue("Avalonia.Data.Binding", out var origBindingType))
			{
				//avalonia 0.10 has implicit binding extension
				bindingExtType = origBindingType with
				{
					Name = "BindingExtension",
					FullName = "Avalonia.Markup.Xaml.MarkupExtensions.BindingExtension"
				};
				bindingExtType.IsMarkupExtension = true;

				types.Add(bindingExtType.FullName, bindingExtType);
				metadata.AddType(Utils.AvaloniaNamespace, bindingExtType);
			}
		}

		avaloniaBaseType.TryGetValue("Avalonia.Controls.Control", out var controlType);
		types.TryGetValue(typeof(Type).FullName!, out var typeType);

		var dataContextType = new MetadataType("{BindingPath}")
		{
			FullName = "{BindingPath}",
			HasHintValues = true,
			HintValues = ["$parent", "$parent[", "$self"]
		};

		//bindings related hints
		if (types.TryGetValue("Avalonia.Markup.Xaml.MarkupExtensions.BindingExtension", out var bindingType))
		{
			bindingType.SupportCtorArgument = MetadataTypeCtorArgument.None;
			for (var i = 0; i < bindingType.Properties.Count; i++)
			{
				if (bindingType.Properties[i].Name == "Path")
				{
					bindingType.Properties[i] = bindingType.Properties[i] with
					{
						Type = dataContextType
					};
				}
			}

			bindingType.Properties.Add(new MetadataProperty("", dataContextType, bindingType, false, false, true, true));
		}

		if (avaloniaBaseType.TryGetValue("Avalonia.Data.TemplateBinding", out var templBinding))
		{
			var tbext = new MetadataType("TemplateBindingExtension")
			{
				IsMarkupExtension = true,
				Properties = templBinding.Properties,
				SupportCtorArgument = MetadataTypeCtorArgument.HintValues,
				HasHintValues = allAvaloniaProps?.Any() ?? false,
				HintValues = allAvaloniaProps
			};

			types["TemplateBindingExtension"] = tbext;
			metadata.AddType(Utils.AvaloniaNamespace, tbext);
		}

		if (avaloniaBaseType.TryGetValue("Portable.Xaml.Markup.TypeExtension", out var typeExtension))
		{
			typeExtension.SupportCtorArgument = MetadataTypeCtorArgument.Type;
		}

		//TODO: may be make it to load from assembly resources
		var commonResKeys = new[]
		{
			//common brushes
			"ThemeBackgroundBrush", "ThemeBorderLowBrush", "ThemeBorderMidBrush", "ThemeBorderHighBrush",
			"ThemeControlLowBrush", "ThemeControlMidBrush", "ThemeControlHighBrush",
			"ThemeControlHighlightLowBrush", "ThemeControlHighlightMidBrush", "ThemeControlHighlightHighBrush",
			"ThemeForegroundBrush", "ThemeForegroundLowBrush", "HighlightBrush",
			"ThemeAccentBrush", "ThemeAccentBrush2", "ThemeAccentBrush3", "ThemeAccentBrush4",
			"ErrorBrush", "ErrorLowBrush",
			//some other usefull
			"ThemeBorderThickness", "ThemeDisabledOpacity",
			"FontSizeSmall", "FontSizeNormal", "FontSizeLarge"
		};

		if (avaloniaBaseType.TryGetValue("Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension", out var dynRes))
		{
			dynRes.SupportCtorArgument = MetadataTypeCtorArgument.HintValues;
			dynRes.HasHintValues = true;
			dynRes.HintValues = commonResKeys;
		}

		if (avaloniaBaseType.TryGetValue("Avalonia.Markup.Xaml.MarkupExtensions.StaticResourceExtension", out var stRes))
		{
			stRes.SupportCtorArgument = MetadataTypeCtorArgument.HintValues;
			stRes.HasHintValues = true;
			stRes.HintValues = commonResKeys;
		}

		//brushes
		if (types.TryGetValue("Avalonia.Media.IBrush", out var brushType) &&
			avaloniaBaseType.TryGetValue("Avalonia.Media.Brushes", out var brushes))
		{
			brushType.HasHintValues = true;
			brushType.HintValues = brushes.Properties.Where(p => p.IsStatic && p.HasGetter).Select(p => p.Name).ToArray();
		}

		//TODO: Remove
		if (avaloniaBaseType.TryGetValue("Avalonia.Styling.Selector", out var styleSelector))
		{
			styleSelector.HasHintValues = true;
			styleSelector.IsCompositeValue = true;

			var hints = new List<string>();

			//some reserved words
			hints.AddRange(["/template/", ":is()", ">", "#", ".", "^", ":not()"]);

			//some pseudo classes
			hints.AddRange(pseudoclasses);

			hints.AddRange(types.Where(t => t.Value.IsAvaloniaObjectType).Select(t => t.Value.Name.Replace(":", "|")));

			styleSelector.HintValues = hints.ToArray();
		}

		var bitmaptypes = new[] { ".jpg", ".bmp", ".png", ".ico" };

		bool Isbitmaptype(string resource)
		{
			return bitmaptypes.Any(ext => Rhasext(resource, ext));
		}

		if (avaloniaBaseType.TryGetValue("Avalonia.Media.Imaging.IBitmap", out var ibitmapType))
		{
			ibitmapType.HasHintValues = true;
			ibitmapType.HintValues = allresourceUrls.Where(r => Isbitmaptype(r)).ToArray();
			ibitmapType.XamlContextHintValuesFunc = (a, t, p) => FilterLocalRes(ibitmapType, a);
		}

		if (avaloniaBaseType.TryGetValue("Avalonia.Media.IImage", out var iImageType))
		{
			iImageType.HasHintValues = true;
			iImageType.HintValues = allresourceUrls.Where(r => Isbitmaptype(r)).ToArray();
			iImageType.XamlContextHintValuesFunc = (a, t, p) => FilterLocalRes(iImageType, a);
		}

		if (avaloniaBaseType.TryGetValue("Avalonia.Controls.WindowIcon", out var winIcon))
		{
			winIcon.HasHintValues = true;
			winIcon.HintValues = allresourceUrls.Where(r => Rhasext(r, ".ico")).ToArray();
			winIcon.XamlContextHintValuesFunc = (a, t, p) => FilterLocalRes(winIcon, a);
		}

		if (avaloniaBaseType.TryGetValue("Avalonia.Markup.Xaml.Styling.StyleInclude", out var styleIncludeType))
		{
			var source = styleIncludeType.Properties.FirstOrDefault(p => p.Name == "Source");

			for (var i = 0; i < styleIncludeType.Properties.Count; i++)
			{
				if (styleIncludeType.Properties[i].Name == "Source")
				{
					styleIncludeType.Properties[i] = styleIncludeType.Properties[i] with
					{
						Type = styleResType
					};
				}
			}
		}

		if (types.TryGetValue("Avalonia.Markup.Xaml.Styling.StyleIncludeExtension", out var styleIncludeExtType))
		{
			var source = styleIncludeExtType.Properties.FirstOrDefault(p => p.Name == "Source");

			for (var i = 0; i < styleIncludeExtType.Properties.Count; i++)
			{
				if (styleIncludeExtType.Properties[i].Name == "Source")
				{
					styleIncludeExtType.Properties[i] = styleIncludeExtType.Properties[i] with
					{
						Type = xamlResType
					};
				}
			}
		}

		if (types.TryGetValue(typeof(Uri).FullName!, out var uriType))
		{
			uriType.HasHintValues = true;
			uriType.HintValues = allresourceUrls.ToArray();
			uriType.XamlContextHintValuesFunc = (a, t, p) => FilterLocalRes(uriType, a);
		}

		if (typeType != null)
		{
			var typeArguments = new MetadataType("TypeArguments")
			{
				IsXamlDirective = true,
				IsValidForXamlContextFunc = (a, t, p) => t?.IsGeneric == true,
				Properties = { new MetadataProperty("", typeType, null, false, false, false, true) }
			};

			metadata.AddType(Utils.Xaml2006Namespace, typeArguments);
		}
	}

	private static void PreProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata)
	{
		MetadataType xDataType, xCompiledBindings, boolType, typeType, int32Type;
		var toAdd = new List<MetadataType>
		{
			(boolType = new MetadataType(typeof(bool).FullName!)
			{
				HasHintValues = true,
				HintValues = ["True", "False"]
			}),
			new("System.Nullable`1<System.Boolean>")
			{
				HasHintValues = true,
				IsNullable = true,
				UnderlyingType = boolType
			},
			new(typeof(Uri).FullName!),
			(typeType = new MetadataType(typeof(Type).FullName!)),
			new("Avalonia.Media.IBrush"),
			new("Avalonia.Media.Imaging.IBitmap"),
			new("Avalonia.Media.IImage"),
			(int32Type = new MetadataType(typeof(int).FullName!)
			{
				HasHintValues = false
			}),
			new("System.Nullable`1<System.Int32>")
			{
				HasHintValues = false,
				IsNullable = true,
				UnderlyingType = int32Type
			}
		};

		foreach (var t in toAdd)
		{
			types.Add(t.Name, t);
		}

		var portableXamlExtTypes = new[]
		{
			new MetadataType("StaticExtension")
			{
				SupportCtorArgument = MetadataTypeCtorArgument.Object,
				HasSetProperties = true,
				IsMarkupExtension = true
			},
			new MetadataType("TypeExtension")
			{
				SupportCtorArgument = MetadataTypeCtorArgument.TypeAndObject,
				HasSetProperties = true,
				IsMarkupExtension = true
			},
			new MetadataType("NullExtension")
			{
				HasSetProperties = true,
				IsMarkupExtension = true
			},
			new MetadataType("Class")
			{
				IsXamlDirective = true
			},
			new MetadataType("Name")
			{
				IsXamlDirective = true
			},
			new MetadataType("Key")
			{
				IsXamlDirective = true
			},
			xDataType = new MetadataType("DataType")
			{
				IsXamlDirective = true,
				Properties = { new MetadataProperty("", typeType, null, false, false, false, true) }
			},
			xCompiledBindings = new MetadataType("CompileBindings")
			{
				IsXamlDirective = true,
				Properties = { new MetadataProperty("", boolType, null, false, false, false, true) }
			},
			new MetadataType("True")
			{
				HasSetProperties = true,
				IsMarkupExtension = true
			},
			new MetadataType("False")
			{
				HasSetProperties = true,
				IsMarkupExtension = true
			}
		};

		//as in avalonia 0.9 Portablexaml is missing we need to hardcode some extensions
		foreach (var t in portableXamlExtTypes)
		{
			metadata.AddType(Utils.Xaml2006Namespace, t);
		}

		types.Add(xDataType.Name, xDataType);
		types.Add(xCompiledBindings.Name, xCompiledBindings);

		//metadata.AddType("", new MetadataType("xmlns") { IsXamlDirective = true });
	}

	private static void ProcessAvaloniaResources(IAssemblyInformation asm, ITypeInformation[] asmTypes, List<AvaresInfo> avaresValues)
	{
		const string avaresToken = "Build:"; //or "Populate:" should work both ways

		void Registeravares(string? localUrl, string returnTypeFullName = "")
		{
			if (localUrl is null)
			{
				return;
			}

			var globalUrl = $"avares://{asm.Name}{localUrl}";

			if (!avaresValues.Any(v => v.GlobalUrl == globalUrl))
			{
				var avres = new AvaresInfo(asm, returnTypeFullName, localUrl, globalUrl);

				avaresValues.Add(avres);
			}
		}

		var resType = asmTypes.FirstOrDefault(t => t.FullName == "CompiledAvaloniaXaml.!AvaloniaResources");
		if (resType != null)
		{
			foreach (var res in resType.Methods.Where(m => m.Name.StartsWith(avaresToken)))
			{
				Registeravares(res.Name.Replace(avaresToken, ""), res.ReturnTypeFullName ?? "");
			}
		}

		//try add avares Embedded resources like image,stream and x:Class
		if (asm.ManifestResourceNames.Contains("!AvaloniaResources"))
		{
			try
			{
				using var avaresStream = asm.GetManifestResourceStream("!AvaloniaResources");
				using var r = new BinaryReader(avaresStream);
				var ms = new MemoryStream(r.ReadBytes(r.ReadInt32()));
				var br = new BinaryReader(ms);

				AvaloniaResourcesIndexEntry[] avaResEntries;

				var version = br.ReadInt32();
				switch (version)
				{
					case LegacyXmlResourceIndex: // Legacy Xml formart
					{
						var assetDoc = XDocument.Load(ms);
						if (assetDoc.Root is null)
						{
							return;
						}
						var ns = assetDoc.Root.GetDefaultNamespace();
						avaResEntries = assetDoc.Root.Element(ns.GetName("Entries"))?.Elements(ns.GetName("AvaloniaResourcesIndexEntry"))
							.Select(entry => new AvaloniaResourcesIndexEntry(entry.Element(ns.GetName("Path"))?.Value,
								int.Parse(entry.Element(ns.GetName("Offset"))?.Value ?? "0"),
								int.Parse(entry.Element(ns.GetName("Size"))?.Value ?? "0")
							)).ToArray() ?? [];
						break;
					}
					case CurrentResourceIndex: // Binary Formart
						var entryCount = br.ReadInt32();
						avaResEntries = new AvaloniaResourcesIndexEntry[entryCount];
						for (var i = 0; i < entryCount; ++i)
						{
							avaResEntries[i] = new(br.ReadString(),
								br.ReadInt32(),
								br.ReadInt32());
						}
						break;
					default:
						throw new NotSupportedException("Invalid Resource Format");
				}

				if (avaResEntries?.FirstOrDefault(v => v.Path == "/!AvaloniaResourceXamlInfo") is AvaloniaResourcesIndexEntry xClassEntries)
				{
					try
					{
						avaresStream.Seek(xClassEntries.Offset, SeekOrigin.Current);
						var xClassDoc = XDocument.Load(new MemoryStream(r.ReadBytes(xClassEntries.Size)));
						var xClassMappingNode = xClassDoc.Root?.Element(xClassDoc.Root.GetDefaultNamespace().GetName("ClassToResourcePathIndex"));
						if (xClassMappingNode != null)
						{
							const string arraysNs = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
							var keyvalueofss = XName.Get("KeyValueOfstringstring", arraysNs);
							var keyName = XName.Get("Key", arraysNs);
							var valueName = XName.Get("Value", arraysNs);

							var xClassMappings = xClassMappingNode.Elements(keyvalueofss)
								.Where(e => e.Elements(keyName).Any() && e.Elements(valueName).Any())
								.Select(e => new
								{
									Type = e.Element(keyName)?.Value,
									Path = e.Element(valueName)?.Value
								}).ToArray();

							foreach (var xcm in xClassMappings)
							{
								var resultType = asmTypes.FirstOrDefault(t => t.FullName == xcm.Type);
								//if we need another check
								//if (resultType?.Methods?.Any(m => m.Name == "!XamlIlPopulate") ?? false)
								if (resultType != null)
								{
									//we set here base class like Style, Styles, UserControl so we can manage
									//resources in a common way later
									Registeravares(xcm.Path, resultType.GetBaseType()?.FullName ?? "");
								}
							}
						}
					}
					catch (Exception xClassEx)
					{
						Console.WriteLine($"Failed fetch avalonia x:class resources in {asm.Name}, {xClassEx.Message}");
					}
				}

				//add other img/stream resources
				if (avaResEntries is not null)
				{
					foreach (var entry in avaResEntries.Where(v => v.Path is not null && !v.Path.StartsWith("/!")))
					{
						Registeravares(entry.Path);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed fetch avalonia resources in {asm.Name}, {ex.Message}");
			}
		}
	}

	private static void ProcessCustomAttributes(IAssemblyInformation asm, Dictionary<string, string[]> aliases)
	{
		foreach (
			var attr in
			asm.CustomAttributes.Where(a => (a.TypeFullName == "Avalonia.Metadata.XmlnsDefinitionAttribute") ||
				(a.TypeFullName == "Portable.Xaml.Markup.XmlnsDefinitionAttribute")))
		{
			var ns = attr.ConstructorArguments[1].Value?.ToString();
			var val = attr.ConstructorArguments[0].Value?.ToString();
			if (ns is null || val is null)
			{
				continue;
			}

			var current = new[] { val };

			if (aliases.TryGetValue(ns, out var allns))
			{
				allns = allns.Union(current).Distinct().ToArray();
			}

			aliases[ns] = allns ?? current;
		}
	}

	private static void ProcessWellKnownAliases(IAssemblyInformation asm, Dictionary<string, string[]> aliases)
	{
		//look like we don't have xmlns for avalonia.layout TODO: add it in avalonia
		//may be don 't remove it for avalonia 0.7 or below for support completion for layout enums etc.
		aliases["Avalonia.Layout"] = ["https://github.com/avaloniaui"];
	}

	#endregion

	#region Records

	private record AvaresInfo(
		IAssemblyInformation Assembly,
		string ReturnTypeFullName,
		string LocalUrl,
		string GlobalUrl)
	{
		#region Methods

		public override string ToString()
		{
			return GlobalUrl;
		}

		#endregion
	}

	#endregion
}