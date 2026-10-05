#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;

#endregion

namespace Cornerstone.VisualStudio.EditorHost;

/// <summary>
/// One converted metadata graph per assembly file. The key is the file fingerprint,
/// so a shared assembly is parsed once until its size or write time changes.
/// </summary>
internal static class AssemblyFragmentStore
{
	#region Fields

	private static readonly object Gate;
	private static readonly Dictionary<string, Entry> Memory;
	private static readonly Dictionary<string, TaskCompletionSource<Metadata>> Inflight;

	#endregion

	#region Constructors

	static AssemblyFragmentStore()
	{
		Gate = new object();
		Memory = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
		Inflight = new Dictionary<string, TaskCompletionSource<Metadata>>(StringComparer.Ordinal);
	}

	#endregion

	#region Methods

	public static bool TryMemory(string path, string fingerprint, out Metadata metadata)
	{
		lock (Gate)
		{
			Entry entry;
			if (Memory.TryGetValue(path, out entry) &&
				string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
			{
				metadata = entry.Metadata;
				return true;
			}
		}

		metadata = null;
		return false;
	}

	public static void Remember(string path, string fingerprint, Metadata metadata)
	{
		lock (Gate)
		{
			Memory[path] = new Entry(fingerprint, metadata);
		}
	}

	public static Task<Metadata> GetOrClaim(string path, string fingerprint, out bool owned)
	{
		var key = Key(path, fingerprint);
		lock (Gate)
		{
			Entry entry;
			if (Memory.TryGetValue(path, out entry) &&
				string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
			{
				owned = false;
				return Task.FromResult(entry.Metadata);
			}

			TaskCompletionSource<Metadata> pending;
			if (Inflight.TryGetValue(key, out pending))
			{
				owned = false;
				return pending.Task;
			}

			pending = new TaskCompletionSource<Metadata>(TaskCreationOptions.RunContinuationsAsynchronously);
			Inflight[key] = pending;
			owned = true;
			return pending.Task;
		}
	}

	public static void Complete(string path, string fingerprint, Metadata metadata)
	{
		TaskCompletionSource<Metadata> pending = null;
		lock (Gate)
		{
			Memory[path] = new Entry(fingerprint, metadata);
			var key = Key(path, fingerprint);
			if (Inflight.TryGetValue(key, out pending))
			{
				Inflight.Remove(key);
			}
		}

		if (pending != null)
		{
			pending.TrySetResult(metadata);
		}
	}

	public static void Fail(string path, string fingerprint, Exception exception)
	{
		TaskCompletionSource<Metadata> pending = null;
		lock (Gate)
		{
			var key = Key(path, fingerprint);
			if (Inflight.TryGetValue(key, out pending))
			{
				Inflight.Remove(key);
			}
		}

		if (pending != null)
		{
			pending.TrySetException(exception);
		}
	}

	private static string Key(string path, string fingerprint)
	{
		return path + "\n" + fingerprint;
	}

	#endregion

	#region Nested

	private sealed class Entry
	{
		public Entry(string fingerprint, Metadata metadata)
		{
			Fingerprint = fingerprint;
			Metadata = metadata;
		}

		public string Fingerprint { get; }

		public Metadata Metadata { get; }
	}

	#endregion
}
