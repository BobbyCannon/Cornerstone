#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;

#endregion

namespace Cornerstone.VisualStudio.EditorHost;

/// <summary>
/// Writes one assembly's metadata graph into the solution .cscache folder.
/// Every project in the solution uses that folder. Version 3 files are per assembly path.
/// Older files are ignored so a stub written for a second target framework is not reused.
/// </summary>
internal static class MetadataDiskCache
{
	#region Constants

	private const int Magic = 0x43534331;
	private const int Version = 3;
	private const int FlagAbstract = 1;
	private const int FlagAvaloniaObject = 2;
	private const int FlagComposite = 4;
	private const int FlagEnum = 8;
	private const int FlagGeneric = 16;
	private const int FlagMarkup = 32;
	private const int FlagNullable = 64;
	private const int FlagStatic = 128;
	private const int FlagXamlDirective = 256;
	private const int FlagAttachedEvents = 512;
	private const int FlagAttachedProperties = 1024;
	private const int FlagHintValues = 2048;
	private const int FlagPseudo = 4096;
	private const int FlagSetProperties = 8192;
	private const int FlagStaticGet = 16384;
	private const int FlagContextHints = 32768;
	private const int FlagGenericContext = 65536;

	#endregion

	#region Methods

	public static bool TryRead(string directory, string identity, string fingerprint, out Metadata metadata)
	{
		metadata = null;
		try
		{
			var path = CachePath(directory, identity);
			if (!File.Exists(path))
			{
				return false;
			}

			using (var stream = File.OpenRead(path))
			using (var reader = new BinaryReader(stream, Encoding.UTF8))
			{
				if ((reader.ReadInt32() != Magic) || (reader.ReadInt32() != Version))
				{
					return false;
				}

				if (!string.Equals(ReadString(reader), fingerprint, StringComparison.Ordinal))
				{
					return false;
				}

				metadata = ReadMetadata(reader);
				return true;
			}
		}
		catch (Exception)
		{
			metadata = null;
			return false;
		}
	}

	public static void TryWrite(string directory, string identity, string fingerprint, Metadata metadata)
	{
		try
		{
			if ((metadata == null) || string.IsNullOrEmpty(directory))
			{
				return;
			}

			Directory.CreateDirectory(directory);
			var path = CachePath(directory, identity);
			var temporary = path + ".tmp";
			using (var stream = File.Create(temporary))
			using (var writer = new BinaryWriter(stream, Encoding.UTF8))
			{
				writer.Write(Magic);
				writer.Write(Version);
				WriteString(writer, fingerprint);
				WriteMetadata(writer, metadata);
			}

			if (File.Exists(path))
			{
				File.Delete(path);
			}

			File.Move(temporary, path);
		}
		catch (Exception)
		{
			// A cache miss next time is safe. The in-memory graph is already built.
		}
	}

	private static string CachePath(string directory, string identity)
	{
		string name;
		using (var sha = SHA256.Create())
		{
			var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(identity ?? string.Empty));
			var builder = new StringBuilder(16);
			for (var i = 0; i < 8; i++)
			{
				builder.Append(hash[i].ToString("x2"));
			}

			name = builder.ToString();
		}

		return Path.Combine(directory, name + ".cscache");
	}

	private static void WriteMetadata(BinaryWriter writer, Metadata metadata)
	{
		var types = new List<MetadataType>();
		var ids = new Dictionary<MetadataType, int>();
		Collect(metadata, types, ids);
		writer.Write(types.Count);
		for (var i = 0; i < types.Count; i++)
		{
			WriteType(writer, types[i], ids);
		}

		writer.Write(metadata.Namespaces.Count);
		foreach (var ns in metadata.Namespaces)
		{
			WriteString(writer, ns.Key);
			writer.Write(ns.Value.Count);
			foreach (var type in ns.Value.Values)
			{
				int id;
				writer.Write(ids.TryGetValue(type, out id) ? id : -1);
			}
		}
	}

	private static void Collect(Metadata metadata, List<MetadataType> types, Dictionary<MetadataType, int> ids)
	{
		foreach (var ns in metadata.Namespaces.Values)
		{
			foreach (var type in ns.Values)
			{
				Add(type);
			}
		}

		void Add(MetadataType type)
		{
			if ((type == null) || ids.ContainsKey(type))
			{
				return;
			}

			ids.Add(type, types.Count);
			types.Add(type);
			Add(type.UnderlyingType);
			if (type.Properties != null)
			{
				for (var i = 0; i < type.Properties.Count; i++)
				{
					Add(type.Properties[i].Type);
					Add(type.Properties[i].DeclaringType);
				}
			}

			if (type.Events != null)
			{
				for (var i = 0; i < type.Events.Count; i++)
				{
					Add(type.Events[i].Type);
					Add(type.Events[i].DeclaringType);
				}
			}

			if (type.TemplateParts != null)
			{
				for (var i = 0; i < type.TemplateParts.Count; i++)
				{
					Add(type.TemplateParts[i].Type);
				}
			}
		}
	}

	private static void WriteType(BinaryWriter writer, MetadataType type, Dictionary<MetadataType, int> ids)
	{
		WriteString(writer, type.Name);
		WriteString(writer, type.FullName);
		WriteString(writer, type.AssemblyQualifiedName);
		writer.Write(Flags(type));
		writer.Write((int) type.SupportCtorArgument);
		WriteStrings(writer, type.HintValues);
		WriteStrings(writer, type.PseudoClasses);
		writer.Write(IdOf(ids, type.UnderlyingType));
		var properties = type.Properties;
		writer.Write(properties == null ? 0 : properties.Count);
		if (properties != null)
		{
			for (var i = 0; i < properties.Count; i++)
			{
				var property = properties[i];
				WriteString(writer, property.Name);
				writer.Write(IdOf(ids, property.Type));
				writer.Write(IdOf(ids, property.DeclaringType));
				writer.Write(property.IsAttached);
				writer.Write(property.IsStatic);
				writer.Write(property.HasGetter);
				writer.Write(property.HasSetter);
			}
		}

		var events = type.Events;
		writer.Write(events == null ? 0 : events.Count);
		if (events != null)
		{
			for (var i = 0; i < events.Count; i++)
			{
				var ev = events[i];
				WriteString(writer, ev.Name);
				writer.Write(IdOf(ids, ev.Type));
				writer.Write(IdOf(ids, ev.DeclaringType));
				writer.Write(ev.IsAttached);
			}
		}

		var parts = type.TemplateParts;
		writer.Write(parts == null ? 0 : parts.Count);
		if (parts != null)
		{
			for (var i = 0; i < parts.Count; i++)
			{
				writer.Write(IdOf(ids, parts[i].Type));
				WriteString(writer, parts[i].Name);
			}
		}
	}

	private static Metadata ReadMetadata(BinaryReader reader)
	{
		var count = reader.ReadInt32();
		var types = new MetadataType[count];
		var flags = new int[count];
		var underlying = new int[count];
		var properties = new List<PropertyRecord>[count];
		var events = new List<EventRecord>[count];
		var parts = new List<PartRecord>[count];
		for (var i = 0; i < count; i++)
		{
			var name = ReadString(reader);
			types[i] = new MetadataType(name ?? string.Empty);
			types[i].FullName = ReadString(reader) ?? string.Empty;
			types[i].AssemblyQualifiedName = ReadString(reader);
			flags[i] = reader.ReadInt32();
			types[i].SupportCtorArgument = (MetadataTypeCtorArgument) reader.ReadInt32();
			types[i].HintValues = ReadStrings(reader);
			types[i].PseudoClasses = ReadStrings(reader) ?? new string[0];
			underlying[i] = reader.ReadInt32();
			ApplyFlags(types[i], flags[i]);
			properties[i] = ReadProperties(reader);
			events[i] = ReadEvents(reader);
			parts[i] = ReadParts(reader);
		}

		for (var i = 0; i < count; i++)
		{
			types[i].UnderlyingType = TypeAt(types, underlying[i]);
			types[i].Properties = new List<MetadataProperty>();
			for (var p = 0; p < properties[i].Count; p++)
			{
				var property = properties[i][p];
				types[i].Properties.Add(new MetadataProperty(
					property.Name,
					TypeAt(types, property.TypeId),
					TypeAt(types, property.DeclaringId),
					property.IsAttached,
					property.IsStatic,
					property.HasGetter,
					property.HasSetter));
			}

			types[i].Events = new List<MetadataEvent>();
			for (var e = 0; e < events[i].Count; e++)
			{
				var ev = events[i][e];
				types[i].Events.Add(new MetadataEvent(
					ev.Name,
					TypeAt(types, ev.TypeId),
					TypeAt(types, ev.DeclaringId),
					ev.IsAttached));
			}

			types[i].TemplateParts = new List<(MetadataType Type, string Name)>();
			for (var p = 0; p < parts[i].Count; p++)
			{
				types[i].TemplateParts.Add((TypeAt(types, parts[i][p].TypeId), parts[i][p].Name));
			}

			AttachFuncs(types[i], flags[i]);
		}

		var metadata = new Metadata();
		var namespaceCount = reader.ReadInt32();
		for (var n = 0; n < namespaceCount; n++)
		{
			var ns = ReadString(reader);
			var typeCount = reader.ReadInt32();
			for (var t = 0; t < typeCount; t++)
			{
				var type = TypeAt(types, reader.ReadInt32());
				if (type != null)
				{
					metadata.AddType(ns, type);
				}
			}
		}

		return metadata;
	}

	private static void AttachFuncs(MetadataType type, int flags)
	{
		if ((flags & FlagContextHints) != 0)
		{
			var captured = type;
			type.XamlContextHintValuesFunc = (assemblyName, ignoredType, ignoredProperty) => FilterLocal(captured, assemblyName);
		}

		if ((flags & FlagGenericContext) != 0)
		{
			type.IsValidForXamlContextFunc = (assemblyName, contextType, ignoredProperty) =>
				(contextType != null) && contextType.IsGeneric;
		}
	}

	private static IEnumerable<string> FilterLocal(MetadataType type, string assemblyName)
	{
		if ((assemblyName == null) || (type.HintValues == null))
		{
			yield break;
		}

		var localResPrefix = "csres://" + assemblyName;
		var resmSuffix = "?assembly=" + assemblyName;
		for (var i = 0; i < type.HintValues.Length; i++)
		{
			var hint = type.HintValues[i];
			if (hint == null)
			{
				continue;
			}

			if (hint.StartsWith("csres://", StringComparison.Ordinal))
			{
				if (hint.StartsWith(localResPrefix, StringComparison.Ordinal))
				{
					yield return hint.Substring(localResPrefix.Length);
				}
			}
			else if (hint.StartsWith("resm:", StringComparison.Ordinal) && hint.EndsWith(resmSuffix, StringComparison.Ordinal))
			{
				yield return hint.Substring(0, hint.Length - resmSuffix.Length);
			}
		}
	}

	private static void ApplyFlags(MetadataType type, int flags)
	{
		type.IsAbstract = (flags & FlagAbstract) != 0;
		type.IsAvaloniaObjectType = (flags & FlagAvaloniaObject) != 0;
		type.IsCompositeValue = (flags & FlagComposite) != 0;
		type.IsEnum = (flags & FlagEnum) != 0;
		type.IsGeneric = (flags & FlagGeneric) != 0;
		type.IsMarkupExtension = (flags & FlagMarkup) != 0;
		type.IsNullable = (flags & FlagNullable) != 0;
		type.IsStatic = (flags & FlagStatic) != 0;
		type.IsXamlDirective = (flags & FlagXamlDirective) != 0;
		type.HasAttachedEvents = (flags & FlagAttachedEvents) != 0;
		type.HasAttachedProperties = (flags & FlagAttachedProperties) != 0;
		type.HasHintValues = (flags & FlagHintValues) != 0;
		type.HasPseudoClasses = (flags & FlagPseudo) != 0;
		type.HasSetProperties = (flags & FlagSetProperties) != 0;
		type.HasStaticGetProperties = (flags & FlagStaticGet) != 0;
	}

	private static int Flags(MetadataType type)
	{
		var flags = 0;
		if (type.IsAbstract) flags |= FlagAbstract;
		if (type.IsAvaloniaObjectType) flags |= FlagAvaloniaObject;
		if (type.IsCompositeValue) flags |= FlagComposite;
		if (type.IsEnum) flags |= FlagEnum;
		if (type.IsGeneric) flags |= FlagGeneric;
		if (type.IsMarkupExtension) flags |= FlagMarkup;
		if (type.IsNullable) flags |= FlagNullable;
		if (type.IsStatic) flags |= FlagStatic;
		if (type.IsXamlDirective) flags |= FlagXamlDirective;
		if (type.HasAttachedEvents) flags |= FlagAttachedEvents;
		if (type.HasAttachedProperties) flags |= FlagAttachedProperties;
		if (type.HasHintValues) flags |= FlagHintValues;
		if (type.HasPseudoClasses) flags |= FlagPseudo;
		if (type.HasSetProperties) flags |= FlagSetProperties;
		if (type.HasStaticGetProperties) flags |= FlagStaticGet;
		if (type.XamlContextHintValuesFunc != null) flags |= FlagContextHints;
		if (type.IsValidForXamlContextFunc != null) flags |= FlagGenericContext;
		return flags;
	}

	private static List<PropertyRecord> ReadProperties(BinaryReader reader)
	{
		var count = reader.ReadInt32();
		var list = new List<PropertyRecord>(count);
		for (var i = 0; i < count; i++)
		{
			list.Add(new PropertyRecord(
				ReadString(reader) ?? string.Empty,
				reader.ReadInt32(),
				reader.ReadInt32(),
				reader.ReadBoolean(),
				reader.ReadBoolean(),
				reader.ReadBoolean(),
				reader.ReadBoolean()));
		}

		return list;
	}

	private static List<EventRecord> ReadEvents(BinaryReader reader)
	{
		var count = reader.ReadInt32();
		var list = new List<EventRecord>(count);
		for (var i = 0; i < count; i++)
		{
			list.Add(new EventRecord(
				ReadString(reader) ?? string.Empty,
				reader.ReadInt32(),
				reader.ReadInt32(),
				reader.ReadBoolean()));
		}

		return list;
	}

	private static List<PartRecord> ReadParts(BinaryReader reader)
	{
		var count = reader.ReadInt32();
		var list = new List<PartRecord>(count);
		for (var i = 0; i < count; i++)
		{
			list.Add(new PartRecord(reader.ReadInt32(), ReadString(reader) ?? string.Empty));
		}

		return list;
	}

	private static MetadataType TypeAt(MetadataType[] types, int id)
	{
		if ((id < 0) || (id >= types.Length))
		{
			return null;
		}

		return types[id];
	}

	private static int IdOf(Dictionary<MetadataType, int> ids, MetadataType type)
	{
		int id;
		return (type != null) && ids.TryGetValue(type, out id) ? id : -1;
	}

	private static void WriteStrings(BinaryWriter writer, string[] values)
	{
		if (values == null)
		{
			writer.Write(0);
			return;
		}

		writer.Write(values.Length);
		for (var i = 0; i < values.Length; i++)
		{
			WriteString(writer, values[i]);
		}
	}

	private static string[] ReadStrings(BinaryReader reader)
	{
		var count = reader.ReadInt32();
		if (count <= 0)
		{
			return new string[0];
		}

		var values = new string[count];
		for (var i = 0; i < count; i++)
		{
			values[i] = ReadString(reader) ?? string.Empty;
		}

		return values;
	}

	private static void WriteString(BinaryWriter writer, string value)
	{
		if (value == null)
		{
			writer.Write(-1);
			return;
		}

		var bytes = Encoding.UTF8.GetBytes(value);
		writer.Write(bytes.Length);
		writer.Write(bytes);
	}

	private static string ReadString(BinaryReader reader)
	{
		var length = reader.ReadInt32();
		if (length < 0)
		{
			return null;
		}

		if (length == 0)
		{
			return string.Empty;
		}

		return Encoding.UTF8.GetString(reader.ReadBytes(length));
	}

	#endregion

	#region Nested

	private struct PropertyRecord
	{
		public PropertyRecord(string name, int typeId, int declaringId, bool isAttached, bool isStatic, bool hasGetter, bool hasSetter)
		{
			Name = name;
			TypeId = typeId;
			DeclaringId = declaringId;
			IsAttached = isAttached;
			IsStatic = isStatic;
			HasGetter = hasGetter;
			HasSetter = hasSetter;
		}

		public int DeclaringId;
		public bool HasGetter;
		public bool HasSetter;
		public bool IsAttached;
		public bool IsStatic;
		public string Name;
		public int TypeId;
	}

	private struct EventRecord
	{
		public EventRecord(string name, int typeId, int declaringId, bool isAttached)
		{
			Name = name;
			TypeId = typeId;
			DeclaringId = declaringId;
			IsAttached = isAttached;
		}

		public int DeclaringId;
		public bool IsAttached;
		public string Name;
		public int TypeId;
	}

	private struct PartRecord
	{
		public PartRecord(int typeId, string name)
		{
			TypeId = typeId;
			Name = name;
		}

		public string Name;
		public int TypeId;
	}

	#endregion
}
