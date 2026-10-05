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
		"Avalonia.Markup.Xaml.Styling.StyleIncludeExtension,",
		"Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.BindingExtension,",
		"Cornerstone.Presentation.Data.Binding,",
		"Cornerstone.Presentation.Controls.Control,",
		"Cornerstone.Presentation.Data.TemplateBinding,",
		"Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.DynamicResourceExtension,",
		"Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.StaticResourceExtension,",
		"Cornerstone.Presentation.Media.Brushes",
		"Cornerstone.Presentation.Styling.Selector,",
		"Cornerstone.Presentation.Media.Imaging.IBitmap",
		"Cornerstone.Presentation.Media.IImage",
		"Cornerstone.Presentation.Controls.Chrome.WindowIcon,",
		"Cornerstone.Presentation.Markup.Xaml.Styling.StyleInclude,"
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
		return ConvertMetadata(provider, null);
	}

	public static Metadata ConvertMetadata(IMetadataReaderSession provider, Action<string, int, int> assemblyProgress)
	{
		var types = new Dictionary<string, MetadataType>();
		var typeDefs = new Dictionary<MetadataType, ITypeInformation>();
		var metadata = new Metadata();
		var resourceUrls = new List<string>();
		var csresValues = new List<CsresInfo>();
		var pseudoclasses = new HashSet<string>();
		var typepseudoclasses = new HashSet<string>();

		var ignoredResExt = new[] { ".resources", ".rd.xml", "!AvaloniaResources" };

		bool SkipRes(string res)
		{
			return ignoredResExt.Any(r => res.EndsWith(r, StringComparison.OrdinalIgnoreCase));
		}

		PreProcessTypes(types, metadata);
		var assemblies = provider.Assemblies.ToArray();
		if (assemblies.Length == 0)
		{
			throw new InvalidOperationException("IMetadataReaderSession.Assemblies list is empty.");
		}

		var targetAssembly = assemblies[0];
		for (var i = 0; i < assemblies.Length; i++)
		{
			var asm = assemblies[i];
			if (assemblyProgress != null)
			{
				assemblyProgress(asm.Name, i + 1, assemblies.Length);
			}

			var aliases = new Dictionary<string, string[]>();

			ProcessWellKnownAliases(asm, aliases);
			ProcessCustomAttributes(asm, aliases);

			Func<ITypeInformation, bool> typeFilter = type => !type.IsInterface && type.IsPublic;

			var includeInternals = provider.IsTargetAssembly(asm);
			if (!includeInternals)
			{
				foreach (var candidate in provider.Assemblies)
				{
					if (provider.IsTargetAssembly(candidate) && IsVisibleTo(asm, candidate))
					{
						includeInternals = true;
						break;
					}
				}
			}

			if (includeInternals)
			{
				typeFilter = type => (type.Name != "<Module>") && !type.IsInterface && !type.IsAbstract;
			}

			var asmTypes = asm.Types.Where(typeFilter).ToArray();

			foreach (var type in asmTypes)
			{
				var mt = types[type.AssemblyQualifiedName] = ConvertTypeInformation(type);
				mt.BaseTypeFullName = type.BaseTypeName ?? string.Empty;
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

			ProcessAvaloniaResources(asm, asmTypes, csresValues);

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
					if (propertyType == null)
					{
						var hintFullName = UnresolvedHintFullName(prop.TypeFullName);
						if (hintFullName != null)
						{
							propertyType = EnsureHintType(hintFullName);
						}
					}

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

				if ((typeDef.FullName == "Avalonia.AvaloniaObject") ||
					(typeDef.FullName == "Cornerstone.Presentation.PresentationObject"))
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

		PostProcessTypes(types, metadata, resourceUrls, csresValues, pseudoclasses);

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

		MetadataType EnsureHintType(string fullName)
		{
			foreach (var existing in types.Values)
			{
				if (existing.FullName == fullName)
				{
					return existing;
				}
			}

			var simpleName = fullName;
			var dot = fullName.LastIndexOf('.');
			if (dot >= 0)
			{
				simpleName = fullName.Substring(dot + 1);
			}

			var created = new MetadataType(simpleName)
			{
				FullName = fullName
			};
			types[fullName] = created;
			return created;
		}

		string UnresolvedHintFullName(string typeName)
		{
			if (NamesType(typeName, "IBrush"))
			{
				return "Cornerstone.Presentation.Media.IBrush";
			}

			if (NamesType(typeName, "IImage"))
			{
				return "Cornerstone.Presentation.Media.IImage";
			}

			if (NamesType(typeName, "IBitmap"))
			{
				return "Cornerstone.Presentation.Media.Imaging.IBitmap";
			}

			if (NamesType(typeName, "TextTrimming"))
			{
				return "Cornerstone.Presentation.Media.TextTrimming";
			}

			if (NamesType(typeName, "CacheMode"))
			{
				return "Cornerstone.Presentation.Media.CacheMode";
			}

			if (NamesType(typeName, "IEffect"))
			{
				return "Cornerstone.Presentation.Media.Effects.IEffect";
			}

			if (NamesType(typeName, "ITransform"))
			{
				return "Cornerstone.Presentation.Media.ITransform";
			}

			if (NamesType(typeName, ".Transform"))
			{
				return "Cornerstone.Presentation.Media.Transform";
			}

			return null;
		}

		bool NamesType(string typeName, string simpleName)
		{
			if (string.IsNullOrEmpty(typeName))
			{
				return false;
			}

			return typeName.EndsWith(simpleName, StringComparison.Ordinal) ||
				typeName.Contains(simpleName + ",") ||
				typeName.Contains(simpleName + "]");
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

		if (type.FullName == "Cornerstone.Presentation.Styling.ThemeVariant")
		{
			mt.HasHintValues = true;
			mt.HintValues = ["Default", "Light", "Dark"];
		}

		return mt;
	}

	private static bool IsVisibleTo(IAssemblyInformation asm, IAssemblyInformation target)
	{
		if ((asm == null) || (target == null))
		{
			return false;
		}

		foreach (var att in asm.InternalsVisibleTo)
		{
			var endNameIndex = att.IndexOf(',');
			var assemblyName = att;
			var targetPublicKey = target.PublicKey ?? string.Empty;
			if (endNameIndex > 0)
			{
				assemblyName = att.Substring(0, endNameIndex);
			}

			if (assemblyName != target.Name)
			{
				continue;
			}

			if (endNameIndex == -1)
			{
				return true;
			}

			var publicKeyIndex = att.IndexOf("PublicKey", endNameIndex, StringComparison.OrdinalIgnoreCase);
			if (publicKeyIndex <= 0)
			{
				continue;
			}

			publicKeyIndex += 9;
			if (publicKeyIndex > att.Length)
			{
				continue;
			}

			while ((publicKeyIndex < att.Length) && (att[publicKeyIndex] == ' ' || att[publicKeyIndex] == '='))
			{
				publicKeyIndex++;
			}

			if (targetPublicKey.Length != (att.Length - publicKeyIndex))
			{
				continue;
			}

			var matches = true;
			for (var i = publicKeyIndex; i < att.Length; i++)
			{
				if (att[i] != targetPublicKey[i - publicKeyIndex])
				{
					matches = false;
					break;
				}
			}

			if (matches)
			{
				return true;
			}
		}

		return false;
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
		IEnumerable<string> resourceUrls, List<CsresInfo> avaResValues, HashSet<string> pseudoclasses)
	{
		bool Rhasext(string resource, string ext)
		{
			return resource.StartsWith("resm:") ? resource.Contains(ext + "?assembly=") : resource.EndsWith(ext);
		}

		var allresourceUrls = avaResValues.Select(v => v.GlobalUrl).Concat(resourceUrls).ToArray();

		var resType = new MetadataType("csres://,resm:")
		{
			IsStatic = true,
			HasHintValues = true,
			HintValues = allresourceUrls
		};

		types.Add(resType.Name, resType);

		var xamlResType = new MetadataType("csres://*.xaml,resm:*.xaml")
		{
			HasHintValues = true,
			HintValues = resType.HintValues.Where(r => Rhasext(r, ".xaml") || Rhasext(r, ".paml") || Rhasext(r, ".cxaml") || Rhasext(r, ".axaml")).ToArray()
		};

		var styleResType = new MetadataType("Style csres://*.xaml,resm:*.xaml")
		{
			HasHintValues = true,
			HintValues = avaResValues.Where(v => v.ReturnTypeFullName.StartsWith("Avalonia.Styling.Style"))
				.Select(v => v.GlobalUrl)
				.Concat(resourceUrls.Where(r => Rhasext(r, ".xaml") || Rhasext(r, ".paml") || Rhasext(r, ".cxaml") || Rhasext(r, ".axaml")))
				.ToArray()
		};

		types.Add(styleResType.Name, styleResType);

		IEnumerable<string> FilterLocalRes(MetadataType type, string? currentAssemblyName)
		{
			if (currentAssemblyName is not null)
			{
				var localResPrefix = $"csres://{currentAssemblyName}";
				var resmSuffix = $"?assembly={currentAssemblyName}";

				foreach (var hint in type.HintValues ?? [])
				{
					if (hint.StartsWith("csres://"))
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
		var defaultXmlns = types.Keys.Any(k => k.StartsWith("Cornerstone.Presentation.", StringComparison.Ordinal))
			? Utils.CornerstoneNamespace
			: Utils.AvaloniaNamespace;

		void AliasBaseType(string avaloniaName, string cornerstoneName)
		{
			if (!avaloniaBaseType.ContainsKey(avaloniaName) &&
				avaloniaBaseType.TryGetValue(cornerstoneName, out var cornerstoneType))
			{
				avaloniaBaseType[avaloniaName] = cornerstoneType;
			}
		}

		AliasBaseType("Avalonia.Markup.Xaml.MarkupExtensions.BindingExtension", "Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.BindingExtension");
		AliasBaseType("Avalonia.Data.Binding", "Cornerstone.Presentation.Data.Binding");
		AliasBaseType("Avalonia.Controls.Control", "Cornerstone.Presentation.Controls.Control");
		AliasBaseType("Avalonia.Data.TemplateBinding", "Cornerstone.Presentation.Data.TemplateBinding");
		AliasBaseType("Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension", "Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.DynamicResourceExtension");
		AliasBaseType("Avalonia.Markup.Xaml.MarkupExtensions.StaticResourceExtension", "Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.StaticResourceExtension");
		AliasBaseType("Avalonia.Media.Brushes", "Cornerstone.Presentation.Media.Brushes");
		AliasBaseType("Avalonia.Styling.Selector", "Cornerstone.Presentation.Styling.Selector");
		AliasBaseType("Avalonia.Media.Imaging.IBitmap", "Cornerstone.Presentation.Media.Imaging.IBitmap");
		AliasBaseType("Avalonia.Media.IImage", "Cornerstone.Presentation.Media.IImage");
		AliasBaseType("Avalonia.Controls.WindowIcon", "Cornerstone.Presentation.Controls.Chrome.WindowIcon");
		AliasBaseType("Avalonia.Markup.Xaml.Styling.StyleInclude", "Cornerstone.Presentation.Markup.Xaml.Styling.StyleInclude");

		if (!types.ContainsKey("Avalonia.Media.IBrush") &&
			types.TryGetValue("Cornerstone.Presentation.Media.IBrush", out var cornerstoneBrush))
		{
			types["Avalonia.Media.IBrush"] = cornerstoneBrush;
		}

		if (!avaloniaBaseType.TryGetValue("Avalonia.Markup.Xaml.MarkupExtensions.BindingExtension", out var bindingExtType) &&
			!avaloniaBaseType.TryGetValue("Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.BindingExtension", out bindingExtType))
		{
			if (avaloniaBaseType.TryGetValue("Avalonia.Data.Binding", out var origBindingType) ||
				avaloniaBaseType.TryGetValue("Cornerstone.Presentation.Data.Binding", out origBindingType))
			{
				// Avalonia 11 has no BindingExtension type; {Binding} is still that name in XAML.
				bindingExtType = origBindingType with
				{
					Name = "BindingExtension",
					FullName = "Avalonia.Markup.Xaml.MarkupExtensions.BindingExtension"
				};
				bindingExtType.IsMarkupExtension = true;
				types[bindingExtType.FullName] = bindingExtType;
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
		if (bindingExtType != null)
		{
			bindingExtType.IsMarkupExtension = true;
			bindingExtType.SupportCtorArgument = MetadataTypeCtorArgument.None;
			for (var i = 0; i < bindingExtType.Properties.Count; i++)
			{
				if (bindingExtType.Properties[i].Name == "Path")
				{
					bindingExtType.Properties[i] = bindingExtType.Properties[i] with
					{
						Type = dataContextType
					};
				}
			}

			bindingExtType.Properties.Add(new MetadataProperty("", dataContextType, bindingExtType, false, false, true, true));
			// Tests use the Avalonia xmlns. Cornerstone documents use the Cornerstone xmlns.
			metadata.AddType(Utils.AvaloniaNamespace, bindingExtType);
			metadata.AddType(Utils.CornerstoneNamespace, bindingExtType);
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
			metadata.AddType(Utils.CornerstoneNamespace, tbext);
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

		//brushes and colors. types is keyed by assembly-qualified name, so match FullName.
		ApplyStaticHints(
			"Cornerstone.Presentation.Media.IBrush",
			"Avalonia.Media.IBrush",
			"Cornerstone.Presentation.Media.Brushes",
			"Avalonia.Media.Brushes");
		ApplyStaticHints(
			"Cornerstone.Presentation.Media.Color",
			"Avalonia.Media.Color",
			"Cornerstone.Presentation.Media.Colors",
			"Avalonia.Media.Colors");
		ApplyStaticHints(
			"Cornerstone.Presentation.Media.TextDecorationCollection",
			"Avalonia.Media.TextDecorationCollection",
			"Cornerstone.Presentation.Media.TextDecorations",
			"Avalonia.Media.TextDecorations");
		ApplyStaticHints(
			"Cornerstone.Presentation.Input.Cursor",
			"Avalonia.Input.Cursor",
			"Cornerstone.Presentation.Input.StandardCursorType",
			"Avalonia.Input.StandardCursorType");
		ApplyFixedHints(
			"Cornerstone.Presentation.Media.TextTrimming",
			"Avalonia.Media.TextTrimming",
			["None", "CharacterEllipsis", "WordEllipsis", "PrefixCharacterEllipsis", "LeadingCharacterEllipsis", "PathSegmentEllipsis"]);
		ApplyFixedHints(
			"Cornerstone.Presentation.Media.CacheMode",
			"Avalonia.Media.CacheMode",
			["BitmapCache"]);
		ApplyFixedHints(
			"Cornerstone.Presentation.Media.ITransform",
			"Avalonia.Media.ITransform",
			["translate", "translateX", "translateY", "scale", "scaleX", "scaleY", "skew", "skewX", "skewY", "rotate", "matrix"]);
		ApplyFixedHints(
			"Cornerstone.Presentation.Media.Transform",
			"Avalonia.Media.Transform",
			["translate", "translateX", "translateY", "scale", "scaleX", "scaleY", "skew", "skewX", "skewY", "rotate", "matrix"]);
		ApplyFixedHints(
			"Cornerstone.Presentation.Media.Effects.IEffect",
			"Avalonia.Media.IEffect",
			["blur(", "drop-shadow("]);

		void ApplyStaticHints(string targetFullName, string targetAlias, string sourceFullName, string sourceAlias)
		{
			var target = FindType(targetFullName) ?? FindType(targetAlias);
			var source = FindType(sourceFullName) ?? FindType(sourceAlias);
			if (target == null)
			{
				types.TryGetValue(targetFullName, out target);
			}

			if (target == null)
			{
				types.TryGetValue(targetAlias, out target);
			}

			if ((target == null) || (source == null))
			{
				return;
			}

			var hints = source.Properties.Where(p => p.IsStatic && p.HasGetter).Select(p => p.Name).ToArray();
			if (hints.Length == 0)
			{
				return;
			}

			// Property types are often the short-name placeholder inserted before the real type.
			StampHintValues(target, hints);
			StampHintValues(FindType(targetAlias), hints);
			if (types.TryGetValue(targetFullName, out var keyedTarget))
			{
				StampHintValues(keyedTarget, hints);
			}

			if (types.TryGetValue(targetAlias, out var keyedAlias))
			{
				StampHintValues(keyedAlias, hints);
			}
		}

		void StampHintValues(MetadataType type, string[] hints)
		{
			if (type == null)
			{
				return;
			}

			type.HasHintValues = true;
			type.HintValues = hints;
			foreach (var wrapper in types.Values)
			{
				if (wrapper.IsNullable && (wrapper.UnderlyingType == type))
				{
					wrapper.HasHintValues = true;
					wrapper.HintValues = hints;
				}
			}
		}

		MetadataType FindType(string fullName)
		{
			foreach (var type in types.Values)
			{
				if (type.FullName == fullName)
				{
					return type;
				}
			}

			return null;
		}

		void ApplyFixedHints(string targetFullName, string targetAlias, string[] hints)
		{
			Apply(FindType(targetFullName));
			Apply(FindType(targetAlias));

			void Apply(MetadataType target)
			{
				if (target == null)
				{
					return;
				}

				target.HasHintValues = true;
				target.HintValues = hints;
				foreach (var wrapper in types.Values)
				{
					if (wrapper.IsNullable && (wrapper.UnderlyingType == target))
					{
						wrapper.HasHintValues = true;
						wrapper.HintValues = hints;
					}
				}
			}
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

		ApplyResourceHints(
			"Avalonia.Media.Imaging.IBitmap",
			"Cornerstone.Presentation.Media.Imaging.IBitmap",
			Isbitmaptype);
		ApplyResourceHints(
			"Avalonia.Media.IImage",
			"Cornerstone.Presentation.Media.IImage",
			Isbitmaptype);
		ApplyResourceHints(
			"Avalonia.Controls.WindowIcon",
			"Cornerstone.Presentation.Controls.Chrome.WindowIcon",
			resource => Rhasext(resource, ".ico"));

		void ApplyResourceHints(string avaloniaName, string cornerstoneFullName, Func<string, bool> match)
		{
			var hints = allresourceUrls.Where(match).ToArray();
			Apply(avaloniaBaseType.TryGetValue(avaloniaName, out var avaloniaType) ? avaloniaType : null);
			Apply(FindType(cornerstoneFullName));

			void Apply(MetadataType target)
			{
				if (target == null)
				{
					return;
				}

				target.HasHintValues = true;
				target.HintValues = hints;
				target.XamlContextHintValuesFunc = (assemblyName, type, property) => FilterLocalRes(target, assemblyName);
				foreach (var wrapper in types.Values)
				{
					if (wrapper.IsNullable && (wrapper.UnderlyingType == target))
					{
						wrapper.HasHintValues = true;
						wrapper.HintValues = hints;
						wrapper.XamlContextHintValuesFunc = target.XamlContextHintValuesFunc;
					}
				}
			}
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
			if (string.IsNullOrEmpty(t.FullName))
			{
				t.FullName = t.Name;
			}

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

	private static void ProcessAvaloniaResources(IAssemblyInformation asm, ITypeInformation[] asmTypes, List<CsresInfo> csresValues)
	{
		const string csresToken = "Build:"; //or "Populate:" should work both ways

		void Registercsres(string? localUrl, string returnTypeFullName = "")
		{
			if (localUrl is null)
			{
				return;
			}

			var globalUrl = $"csres://{asm.Name}{localUrl}";

			if (!csresValues.Any(v => v.GlobalUrl == globalUrl))
			{
				var avres = new CsresInfo(asm, returnTypeFullName, localUrl, globalUrl);

				csresValues.Add(avres);
			}
		}

		var resType = asmTypes.FirstOrDefault(t => t.FullName == "CompiledAvaloniaXaml.!AvaloniaResources");
		if (resType != null)
		{
			foreach (var res in resType.Methods.Where(m => m.Name.StartsWith(csresToken)))
			{
				Registercsres(res.Name.Replace(csresToken, ""), res.ReturnTypeFullName ?? "");
			}
		}

		//try add csres Embedded resources like image,stream and x:Class
		if (asm.ManifestResourceNames.Contains("!AvaloniaResources"))
		{
			try
			{
				using var csresStream = asm.GetManifestResourceStream("!AvaloniaResources");
				using var r = new BinaryReader(csresStream);
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
						csresStream.Seek(xClassEntries.Offset, SeekOrigin.Current);
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
									Registercsres(xcm.Path, resultType.GetBaseType()?.FullName ?? "");
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
						Registercsres(entry.Path);
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
				(a.TypeFullName == "Cornerstone.Presentation.Metadata.XmlnsDefinitionAttribute") ||
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
		aliases["Cornerstone.Presentation.Layout"] = ["https://github.com/BobbyCannon/Cornerstone"];
	}

	#endregion

	#region Records

	private record CsresInfo(
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