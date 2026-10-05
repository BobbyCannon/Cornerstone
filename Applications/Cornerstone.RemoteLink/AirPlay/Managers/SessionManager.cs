#region References

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Cornerstone.RemoteLink.AirPlay.Models;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Managers;

public class SessionManager
{
	#region Fields

	private static SessionManager _current;
	private readonly ConcurrentDictionary<string, Session> _sessions;

	#endregion

	#region Constructors

	private SessionManager()
	{
		_sessions = new ConcurrentDictionary<string, Session>();
	}

	#endregion

	#region Properties

	public static SessionManager Current => _current ?? (_current = new SessionManager());

	#endregion

	#region Methods

	/// <summary>
	/// Store session state. Callers must always retrieve the session via GetSessionAsync first,
	/// modify it, then save it back. The incoming session replaces the old one entirely,
	/// allowing fields to be intentionally set to null (e.g., during TEARDOWN cleanup).
	/// </summary>
	public Task CreateOrUpdateSessionAsync(string key, Session session)
	{
		_sessions.AddOrUpdate(key, session, (k, old) => session);
		return Task.CompletedTask;
	}

	public Task<Session> GetSessionAsync(string key)
	{
		_sessions.TryGetValue(key, out var _session);
		return Task.FromResult(_session ?? new Session(key));
	}

	public async Task StopMirroringSessionsAsync()
	{
		foreach (var pair in _sessions.ToArray())
		{
			var session = pair.Value;
			if (session.MirroringListener != null)
			{
				try
				{
					await session.MirroringListener.StopAsync().ConfigureAwait(false);
				}
				catch
				{
				}

				session.MirroringListener = null;
			}

			session.SpsPps = null;
			session.StreamConnectionId = null;
			session.MirroringSession = null;
			_sessions.TryRemove(pair.Key, out _);
		}
	}

	#endregion
}