#region References

using System;
using System.Collections.Generic;
using Cornerstone.VisualStudio.Protocol;

#endregion

namespace Cornerstone.VisualStudio.EditorHost;

/// <summary>
/// Reports the assembly the metadata reader is working on. The editor server
/// forwards each change to Visual Studio. Parallel reads keep the oldest file
/// on screen until it finishes, so the count advances once per completed file.
/// </summary>
internal static class AssemblyLoadProgress
{
	#region Fields

	private static readonly object Gate;
	private static readonly List<string> InFlight;
	private static int _completed;
	private static string _lastName;
	private static string _phase;
	private static string _published;
	private static int _total;

	#endregion

	#region Constructors

	static AssemblyLoadProgress()
	{
		Gate = new object();
		InFlight = new List<string>();
		_completed = 0;
		_lastName = string.Empty;
		_phase = string.Empty;
		_published = string.Empty;
		_total = 0;
	}

	#endregion

	#region Events

	public static event Action<MetadataProgressMessage> Changed;

	#endregion

	#region Methods

	public static void Begin(int total, string phase)
	{
		lock (Gate)
		{
			InFlight.Clear();
			_completed = 0;
			_lastName = string.Empty;
			_phase = phase ?? string.Empty;
			_published = string.Empty;
			_total = total;
		}
	}

	public static void Report(string fileName, bool starting)
	{
		MetadataProgressMessage message = null;
		lock (Gate)
		{
			var name = fileName ?? string.Empty;
			if (starting)
			{
				InFlight.Add(name);
				if (InFlight.Count != 1)
				{
					return;
				}
			}
			else
			{
				if (!RemoveFirst(name))
				{
					return;
				}

				_completed++;
				_lastName = name;
			}

			message = Capture();
		}

		Publish(message);
	}

	public static void ReportAt(string fileName, int index, int total)
	{
		var message = new MetadataProgressMessage();
		message.AssemblyName = fileName ?? string.Empty;
		message.Index = index;
		message.Starting = 1;
		message.Total = total;
		lock (Gate)
		{
			message.Phase = _phase ?? string.Empty;
			_completed = index;
			_lastName = message.AssemblyName;
			_total = total;
		}

		Publish(message);
	}

	private static MetadataProgressMessage Capture()
	{
		var message = new MetadataProgressMessage();
		message.Phase = _phase ?? string.Empty;
		message.Total = _total;
		if (InFlight.Count > 0)
		{
			message.AssemblyName = InFlight[0];
			var index = _completed + 1;
			if ((_total > 0) && (index > _total))
			{
				index = _total;
			}

			message.Index = index;
			message.Starting = 1;
		}
		else
		{
			message.AssemblyName = _lastName ?? string.Empty;
			message.Index = _completed;
			message.Starting = 0;
		}

		return message;
	}

	private static void Publish(MetadataProgressMessage message)
	{
		if (message == null)
		{
			return;
		}

		lock (Gate)
		{
			var key = message.Index + "\n" + message.Total + "\n" + (message.Phase ?? string.Empty) + "\n" + (message.AssemblyName ?? string.Empty);
			if (string.Equals(_published, key, StringComparison.Ordinal))
			{
				return;
			}

			_published = key;
			var changed = Changed;
			if (changed != null)
			{
				changed(message);
			}
		}
	}

	private static bool RemoveFirst(string name)
	{
		for (var i = 0; i < InFlight.Count; i++)
		{
			if (string.Equals(InFlight[i], name, StringComparison.OrdinalIgnoreCase))
			{
				InFlight.RemoveAt(i);
				return true;
			}
		}

		return false;
	}

	#endregion
}