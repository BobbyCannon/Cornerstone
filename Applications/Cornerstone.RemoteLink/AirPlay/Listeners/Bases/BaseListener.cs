#region References

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Listeners.Bases;

public abstract class BaseListener
{
	#region Methods

	public abstract Task StartAsync(CancellationToken cancellationToken);

	public abstract Task StopAsync();

	#endregion
}