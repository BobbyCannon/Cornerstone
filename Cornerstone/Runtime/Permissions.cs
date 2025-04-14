#region References

using System.Collections;
using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Runtime;

public class Permissions : ReadOnlySpeedyList<Permission>, IPermissions
{
	#region Constructors

	public Permissions(IDispatcher dispatcher)
		: base(new SpeedyList<Permission>(dispatcher)
		{
			DistinctCheck = (x, y) => x.Type == y.Type
		})
	{
	}

	#endregion

	#region Properties

	public PermissionStatus Accelerometer => Get(PermissionType.Accelerometer);

	public PermissionStatus Camera => Get(PermissionType.Camera);

	public PermissionStatus ClipboardRead => Get(PermissionType.ClipboardRead);

	public PermissionStatus ClipboardWrite => Get(PermissionType.ClipboardWrite);

	public PermissionStatus Gyroscope => Get(PermissionType.Gyroscope);

	public PermissionStatus Location => Get(PermissionType.Location);

	public PermissionStatus Magnetometer => Get(PermissionType.Magnetometer);

	public PermissionStatus Microphone => Get(PermissionType.Microphone);

	public PermissionStatus Notifications => Get(PermissionType.Notifications);

	public PermissionStatus Storage => Get(PermissionType.Storage);

	public PermissionStatus Video => Get(PermissionType.Video);

	#endregion

	#region Methods

	public virtual Task<PermissionStatus> CheckPermissionAsync(PermissionType type)
	{
		return Task.FromResult(PermissionStatus.Unknown);
	}

	public async Task RefreshAsync()
	{
		var keys = EnumExtensions.GetEnumValues(PermissionType.Unknown);

		List.Clear();

		foreach (var key in keys)
		{
			await GetOrCache(key);
		}
	}

	public virtual Task<PermissionStatus> RequestPermissionAsync(PermissionType type)
	{
		return Task.FromResult(PermissionStatus.Unknown);
	}

	protected PermissionStatus AddOrUpdateCache(PermissionType type, PermissionStatus status)
	{
		var permission = List.FirstOrDefault(x => x.Type == type);
		if (permission != null)
		{
			return permission.Status = status;
		}

		permission = new Permission(this) { Type = type, Status = status };
		List.Add(permission);
		return permission.Status;
	}

	protected async Task<PermissionStatus> GetOrCache(PermissionType type)
	{
		var permission = List.FirstOrDefault(x => x.Type == type);
		if (permission != null)
		{
			return permission.Status;
		}

		permission = new Permission(this) { Type = type, Status = await CheckPermissionAsync(type) };
		List.Add(permission);
		return permission.Status;
	}

	private PermissionStatus Get(PermissionType type)
	{
		return List.FirstOrDefault(x => x.Type == type)?.Status ?? PermissionStatus.Unknown;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}

public interface IPermissions : ISpeedyList<Permission>
{
	#region Properties

	PermissionStatus Accelerometer { get; }

	PermissionStatus Camera { get; }

	PermissionStatus ClipboardRead { get; }

	PermissionStatus ClipboardWrite { get; }

	PermissionStatus Gyroscope { get; }

	PermissionStatus Location { get; }

	PermissionStatus Magnetometer { get; }

	PermissionStatus Microphone { get; }

	PermissionStatus Notifications { get; }

	PermissionStatus Storage { get; }

	PermissionStatus Video { get; }

	#endregion

	#region Methods

	Task<PermissionStatus> CheckPermissionAsync(PermissionType type);

	Task RefreshAsync();

	Task<PermissionStatus> RequestPermissionAsync(PermissionType type);

	#endregion
}