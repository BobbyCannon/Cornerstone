#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.DnlibMetadataProvider;

#endregion

namespace Cornerstone.VisualStudio.EditorHost;

/// <summary>
/// One assembly cache per solution, shared by every project in that solution.
/// Each assembly is stored by its own fingerprint and is read again only when that file changes.
/// </summary>
internal static class MetadataCache
{
	#region Fields

	private static readonly object Gate;
	private static readonly SemaphoreSlim LoadGate;
	private static readonly Dictionary<string, Entry> Entries;
	private static string _directory;

	#endregion

	#region Constructors

	static MetadataCache()
	{
		Gate = new object();
		var parallelism = Environment.ProcessorCount;
		if (parallelism < 2)
		{
			parallelism = 2;
		}

		if (parallelism > 8)
		{
			parallelism = 8;
		}

		LoadGate = new SemaphoreSlim(parallelism, parallelism);
		Entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
		_directory = null;
	}

	#endregion

	#region Methods

	public static void UseSolutionDirectory(string solutionDirectory)
	{
		if (string.IsNullOrWhiteSpace(solutionDirectory))
		{
			return;
		}

		lock (Gate)
		{
			_directory = Path.Combine(solutionDirectory, ".cscache");
		}
	}

	public static Task<MetadataCacheHit> GetHitAsync(IList<string> assemblyPaths, IList<string> targetPaths)
	{
		var paths = Normalize(assemblyPaths);
		if (paths.Length == 0)
		{
			return Task.FromResult(new MetadataCacheHit(new Metadata(), false, 0, 0));
		}

		string identity;
		string fingerprint;
		SetIdentity(paths, out identity, out fingerprint);
		lock (Gate)
		{
			Entry existing;
			Entries.TryGetValue(identity, out existing);
			if ((existing != null) &&
				string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal) &&
				!existing.Load.IsFaulted &&
				!existing.Load.IsCanceled)
			{
				// A finished load keeps its original read count. A later check that
				// finds the same files must report that nothing was read again.
				if (existing.Load.Status == TaskStatus.RanToCompletion)
				{
					var finishedHit = existing.Load.Result;
					return Task.FromResult(new MetadataCacheHit(finishedHit.Metadata, true, 0, 0));
				}

				return existing.Load;
			}

			var known = existing == null ? null : existing.Ready;
			var load = LoadAsync(paths, Normalize(targetPaths));
			var created = new Entry(paths, fingerprint, load, known);
			Entries[identity] = created;
			load.ContinueWith(
				finished =>
				{
					lock (Gate)
					{
						Entry current;
						if (!Entries.TryGetValue(identity, out current) || !ReferenceEquals(current.Load, finished))
						{
							return;
						}

						if (finished.IsFaulted || finished.IsCanceled)
						{
							if (current.Ready == null)
							{
								Entries.Remove(identity);
							}

							return;
						}

						current.Ready = finished.Result.Metadata;
					}
				},
				CancellationToken.None,
				TaskContinuationOptions.None,
				TaskScheduler.Default);
			return load;
		}
	}

	/// <summary>
	/// Metadata already finished for this assembly set. A load still running does not block;
	/// the previous finished graph is returned when one exists.
	/// </summary>
	public static bool TryGetReady(IList<string> assemblyPaths, out Metadata metadata)
	{
		metadata = null;
		var paths = Normalize(assemblyPaths);
		if (paths.Length == 0)
		{
			return false;
		}

		string identity;
		string fingerprint;
		SetIdentity(paths, out identity, out fingerprint);
		var refresh = false;
		lock (Gate)
		{
			Entry entry;
			if (!Entries.TryGetValue(identity, out entry))
			{
				refresh = true;
			}
			else if (string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal) &&
				entry.Load.Status == TaskStatus.RanToCompletion)
			{
				metadata = entry.Load.Result.Metadata;
				return metadata != null;
			}
			else if (!string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
			{
				// File size or write time changed. Serve the previous graph for this
				// call and read the assemblies that changed.
				refresh = true;
				metadata = entry.Ready;
			}
			else
			{
				metadata = entry.Ready;
			}
		}

		if (refresh)
		{
			var reloadPaths = paths;
			Task.Run(() => GetHitAsync(reloadPaths, null));
		}

		return metadata != null;
	}

	/// <summary>
	/// Builds a graph from assembly fragments that are already cached. Does not read an assembly
	/// that is not cached and does not wait for a load.
	/// </summary>
	public static bool TryAssembleKnown(IList<string> assemblyPaths, out Metadata metadata)
	{
		metadata = null;
		if (TryGetReady(assemblyPaths, out metadata))
		{
			return true;
		}

		var paths = Normalize(assemblyPaths);
		if (paths.Length == 0)
		{
			return false;
		}

		var directory = CacheDirectory();
		var merged = new Metadata();
		var any = false;
		for (var i = 0; i < paths.Length; i++)
		{
			var fileFingerprint = FileFingerprint(paths[i]);
			Metadata fragment;
			if (!AssemblyFragmentStore.TryMemory(paths[i], fileFingerprint, out fragment) &&
				!TryReadFragment(directory, paths[i], fileFingerprint, out fragment))
			{
				continue;
			}

			if (fragment == null)
			{
				continue;
			}

			merged.MergeFrom(fragment);
			any = true;
		}

		if (!any)
		{
			return false;
		}

		InheritBases(merged);
		metadata = merged;
		return true;
	}

	private static void SetIdentity(string[] paths, out string identity, out string fingerprint)
	{
		var sorted = (string[])paths.Clone();
		Array.Sort(sorted, StringComparer.OrdinalIgnoreCase);
		identity = string.Join("\n", sorted);
		fingerprint = Fingerprint(sorted);
	}

	private static async Task<MetadataCacheHit> LoadAsync(string[] paths, string[] targetPaths)
	{
		var sw = Stopwatch.StartNew();
		var directory = CacheDirectory();
		var owned = new List<OwnedAssembly>();
		try
		{
			var fragments = new Task<Metadata>[paths.Length];
			if (paths.Length > 0)
			{
				AssemblyLoadProgress.Begin(paths.Length, "Checking");
			}

			for (var i = 0; i < paths.Length; i++)
			{
				var path = paths[i];
				AssemblyLoadProgress.ReportAt(Path.GetFileName(path), i + 1, paths.Length);
				var fileFingerprint = FileFingerprint(path);
				Metadata ready;
				if (AssemblyFragmentStore.TryMemory(path, fileFingerprint, out ready) ||
					TryReadFragment(directory, path, fileFingerprint, out ready))
				{
					fragments[i] = Task.FromResult(ready);
					continue;
				}

				bool mine;
				fragments[i] = AssemblyFragmentStore.GetOrClaim(path, fileFingerprint, out mine);
				if (mine)
				{
					owned.Add(new OwnedAssembly(path, fileFingerprint));
				}
			}

			for (var i = 0; i < paths.Length; i++)
			{
				if (!Owns(owned, paths[i]))
				{
					await fragments[i].ConfigureAwait(false);
				}
			}

			if (owned.Count > 0)
			{
				await LoadGate.WaitAsync().ConfigureAwait(false);
				try
				{
					BuildOwned(paths, owned, directory, fragments, targetPaths);
				}
				finally
				{
					LoadGate.Release();
				}
			}

			var merged = new Metadata();
			for (var i = 0; i < fragments.Length; i++)
			{
				var part = await fragments[i].ConfigureAwait(false);
				if (part != null)
				{
					merged.MergeFrom(part);
				}
			}

			InheritBases(merged);
			sw.Stop();
			Trace.WriteLine(
				"Completion metadata ready in " + sw.Elapsed.TotalSeconds.ToString("0.0") +
				"s (" + paths.Length + " assemblies, " + owned.Count + " read)");
			return new MetadataCacheHit(merged, owned.Count == 0, sw.Elapsed.TotalSeconds, owned.Count);
		}
		catch (Exception ex)
		{
			for (var i = 0; i < owned.Count; i++)
			{
				if (!owned[i].Published)
				{
					AssemblyFragmentStore.Fail(owned[i].Path, owned[i].Fingerprint, ex);
				}
			}

			Trace.WriteLine("Completion metadata load failed: " + ex.Message);
			throw;
		}
	}

	private static bool Owns(List<OwnedAssembly> owned, string path)
	{
		for (var i = 0; i < owned.Count; i++)
		{
			if (string.Equals(owned[i].Path, path, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static bool TryReadFragment(string directory, string path, string fingerprint, out Metadata metadata)
	{
		if (!MetadataDiskCache.TryRead(directory, path, fingerprint, out metadata))
		{
			return false;
		}

		AssemblyFragmentStore.Remember(path, fingerprint, metadata);
		return true;
	}

	private static void BuildOwned(string[] setPaths, List<OwnedAssembly> owned, string directory, Task<Metadata>[] fragments, string[] targetPaths)
	{
		var target = setPaths.Length == 0 ? string.Empty : setPaths[0];
		var firstIsTarget = Owns(owned, target);
		var missing = new string[owned.Count];
		var cursor = 0;
		if (firstIsTarget)
		{
			missing[cursor] = target;
			cursor++;
		}

		for (var i = 0; i < owned.Count; i++)
		{
			if (firstIsTarget && string.Equals(owned[i].Path, target, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			missing[cursor] = owned[i].Path;
			cursor++;
		}

		Metadata converted;
		AssemblyLoadProgress.Begin(missing.Length, "Reading");
		using (var session = new DnlibMetadataProvider().GetMetadata(missing, firstIsTarget, AssemblyLoadProgress.Report, targetPaths))
		{
			AssemblyLoadProgress.Begin(session.Assemblies.Count, "Converting");
			converted = MetadataConverter.ConvertMetadata(session, AssemblyLoadProgress.ReportAt);
		}

		var split = SplitByAssembly(converted, missing);
		var known = new Metadata();
		for (var i = 0; i < setPaths.Length; i++)
		{
			if (Owns(owned, setPaths[i]) || !fragments[i].IsCompleted || fragments[i].IsFaulted)
			{
				continue;
			}

			var ready = fragments[i].Result;
			if (ready != null)
			{
				known.MergeFrom(ready);
			}
		}

		for (var i = 0; i < owned.Count; i++)
		{
			Metadata fragment;
			if (!split.TryGetValue(owned[i].Path, out fragment) || (fragment == null))
			{
				fragment = new Metadata();
			}

			known.MergeFrom(fragment);
		}

		InheritBases(known);
		if (owned.Count > 0)
		{
			AssemblyLoadProgress.Begin(owned.Count, "Writing");
		}

		for (var i = 0; i < owned.Count; i++)
		{
			Metadata fragment;
			if (!split.TryGetValue(owned[i].Path, out fragment) || (fragment == null))
			{
				fragment = new Metadata();
			}

			AssemblyLoadProgress.ReportAt(Path.GetFileName(owned[i].Path), i + 1, owned.Count);
			MetadataDiskCache.TryWrite(directory, owned[i].Path, owned[i].Fingerprint, fragment);
			AssemblyFragmentStore.Complete(owned[i].Path, owned[i].Fingerprint, fragment);
			owned[i].Published = true;
		}
	}

	private static string CacheDirectory()
	{
		lock (Gate)
		{
			if (!string.IsNullOrEmpty(_directory))
			{
				return _directory;
			}
		}

		return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Cornerstone",
			"EditorHost",
			"cscache");
	}

	private static string[] Normalize(IList<string> assemblyPaths)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var paths = new List<string>();
		if (assemblyPaths != null)
		{
			for (var i = 0; i < assemblyPaths.Count; i++)
			{
				var path = assemblyPaths[i];
				if (string.IsNullOrWhiteSpace(path))
				{
					continue;
				}

				string full;
				try
				{
					full = Path.GetFullPath(path);
				}
				catch (Exception)
				{
					continue;
				}

				if (seen.Add(full))
				{
					paths.Add(full);
				}
			}
		}

		return paths.ToArray();
	}

	private static string FileFingerprint(string path)
	{
		return Fingerprint(new[] { path });
	}

	private static string Fingerprint(string[] paths)
	{
		var text = new System.Text.StringBuilder();
		for (var i = 0; i < paths.Length; i++)
		{
			var path = paths[i];
			text.Append(path);
			text.Append('|');
			try
			{
				var info = new FileInfo(path);
				if (info.Exists)
				{
					text.Append(info.Length);
					text.Append('|');
					text.Append(info.LastWriteTimeUtc.Ticks);
				}
				else
				{
					text.Append("missing");
				}
			}
			catch (IOException)
			{
				text.Append("unknown");
			}
			catch (UnauthorizedAccessException)
			{
				text.Append("unknown");
			}

			text.Append('\n');
		}

		return text.ToString();
	}

	#endregion

	private static Dictionary<string, Metadata> SplitByAssembly(Metadata converted, string[] paths)
	{
		// net10.0 and net10.0-android both report the assembly name Cornerstone.Presentation.
		// Keeping only the first path stored the controls on that file and wrote a stub for the
		// desktop DLL the designer actually completes against.
		var pathsByName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		for (var i = 0; i < paths.Length; i++)
		{
			var path = paths[i];
			string name;
			try
			{
				name = AssemblyName.GetAssemblyName(path).Name;
			}
			catch (Exception)
			{
				name = Path.GetFileNameWithoutExtension(path);
			}

			if (string.IsNullOrEmpty(name))
			{
				continue;
			}

			List<string> sameName;
			if (!pathsByName.TryGetValue(name, out sameName))
			{
				sameName = new List<string>();
				pathsByName.Add(name, sameName);
			}

			sameName.Add(path);
		}

		var ownersOf = new Dictionary<MetadataType, List<string>>();
		foreach (var ns in converted.Namespaces)
		{
			var key = ns.Key ?? string.Empty;
			var marker = key.LastIndexOf(";assembly=", StringComparison.OrdinalIgnoreCase);
			if (marker < 0)
			{
				continue;
			}

			var assemblyName = key.Substring(marker + ";assembly=".Length);
			List<string> owners;
			if (!pathsByName.TryGetValue(assemblyName, out owners))
			{
				continue;
			}

			foreach (var type in ns.Value.Values)
			{
				if (!ownersOf.ContainsKey(type))
				{
					ownersOf.Add(type, owners);
				}
			}
		}

		var split = new Dictionary<string, Metadata>(StringComparer.OrdinalIgnoreCase);
		foreach (var ns in converted.Namespaces)
		{
			foreach (var type in ns.Value.Values)
			{
				List<string> owners;
				if (!ownersOf.TryGetValue(type, out owners))
				{
					for (var i = 0; i < paths.Length; i++)
					{
						AddFragmentType(split, paths[i], ns.Key, type);
					}

					continue;
				}

				for (var i = 0; i < owners.Count; i++)
				{
					AddFragmentType(split, owners[i], ns.Key, type);
				}
			}
		}

		return split;
	}

	private static void AddFragmentType(Dictionary<string, Metadata> split, string path, string ns, MetadataType type)
	{
		Metadata fragment;
		if (!split.TryGetValue(path, out fragment))
		{
			fragment = new Metadata();
			split.Add(path, fragment);
		}

		fragment.AddType(ns, type);
	}

	private static void InheritBases(Metadata metadata)
	{
		var byFullName = new Dictionary<string, MetadataType>(StringComparer.Ordinal);
		foreach (var ns in metadata.Namespaces.Values)
		{
			foreach (var type in ns.Values)
			{
				if (!string.IsNullOrEmpty(type.FullName) && !byFullName.ContainsKey(type.FullName))
				{
					byFullName.Add(type.FullName, type);
				}
			}
		}

		foreach (var type in byFullName.Values)
		{
			var guard = 0;
			var seen = new HashSet<string>(StringComparer.Ordinal);
			var baseName = type.BaseTypeFullName;
			while (!string.IsNullOrEmpty(baseName) && seen.Add(baseName) && (guard < 32))
			{
				guard++;
				MetadataType baseType;
				if (!byFullName.TryGetValue(baseName, out baseType))
				{
					break;
				}

				CopyMembers(type, baseType);
				baseName = baseType.BaseTypeFullName;
			}
		}
	}

	private static void CopyMembers(MetadataType type, MetadataType baseType)
	{
		lock (type)
		{
			if (baseType.IsAvaloniaObjectType)
			{
				type.IsAvaloniaObjectType = true;
			}

			if (baseType.IsMarkupExtension)
			{
				type.IsMarkupExtension = true;
			}

			if (baseType.Properties != null)
			{
				for (var i = 0; i < baseType.Properties.Count; i++)
				{
					var property = baseType.Properties[i];
					if (!HasProperty(type, property.Name))
					{
						type.Properties.Add(property);
					}
				}
			}

			if (baseType.Events != null)
			{
				for (var i = 0; i < baseType.Events.Count; i++)
				{
					var ev = baseType.Events[i];
					if (!HasEvent(type, ev.Name))
					{
						type.Events.Add(ev);
					}
				}
			}

			type.HasAttachedProperties = false;
			type.HasAttachedEvents = false;
			type.HasStaticGetProperties = false;
			type.HasSetProperties = false;
			if (type.Properties != null)
			{
				for (var i = 0; i < type.Properties.Count; i++)
				{
					var property = type.Properties[i];
					if (property.IsAttached)
					{
						type.HasAttachedProperties = true;
					}

					if (property.IsStatic && property.HasGetter)
					{
						type.HasStaticGetProperties = true;
					}

					if (!property.IsStatic && property.HasSetter)
					{
						type.HasSetProperties = true;
					}
				}
			}

			if (type.Events != null)
			{
				for (var i = 0; i < type.Events.Count; i++)
				{
					if (type.Events[i].IsAttached)
					{
						type.HasAttachedEvents = true;
						break;
					}
				}
			}
		}
	}

	private static bool HasProperty(MetadataType type, string name)
	{
		if ((type.Properties == null) || string.IsNullOrEmpty(name))
		{
			return false;
		}

		for (var i = 0; i < type.Properties.Count; i++)
		{
			if (string.Equals(type.Properties[i].Name, name, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static bool HasEvent(MetadataType type, string name)
	{
		if ((type.Events == null) || string.IsNullOrEmpty(name))
		{
			return false;
		}

		for (var i = 0; i < type.Events.Count; i++)
		{
			if (string.Equals(type.Events[i].Name, name, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	#region Nested

	private sealed class OwnedAssembly
	{
		public OwnedAssembly(string path, string fingerprint)
		{
			Path = path;
			Fingerprint = fingerprint;
			Published = false;
		}

		public string Fingerprint { get; }

		public string Path { get; }

		public bool Published { get; set; }
	}

	internal sealed class MetadataCacheHit
	{
		public MetadataCacheHit(Metadata metadata, bool fromCache, double seconds, int readCount)
		{
			Metadata = metadata;
			FromCache = fromCache;
			Seconds = seconds;
			ReadCount = readCount;
		}

		public bool FromCache { get; }

		public Metadata Metadata { get; }

		public int ReadCount { get; }

		public double Seconds { get; }
	}

	private sealed class Entry
	{
		public Entry(string[] paths, string fingerprint, Task<MetadataCacheHit> load, Metadata ready)
		{
			Paths = paths;
			Fingerprint = fingerprint;
			Load = load;
			Ready = ready;
		}

		public string Fingerprint { get; }

		public Task<MetadataCacheHit> Load { get; }

		public string[] Paths { get; }

		public Metadata Ready { get; set; }
	}

	#endregion
}
