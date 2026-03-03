namespace Cornerstone.Serialization;

public interface IPackable
{
	#region Methods

	void FromSpeedyPacket(SpeedyPacket values);

	SpeedyPacket ToSpeedyPacket();

	#endregion
}