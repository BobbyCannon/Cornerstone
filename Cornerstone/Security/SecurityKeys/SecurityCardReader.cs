#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Security.SecurityKeys.Apdu.Commands;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public class SecurityCardReader : SecurityCard
{
	#region Constructors

	public SecurityCardReader(SmartCardReader connection, int totalSize, IDispatcher dispatcher)
		: base(totalSize, dispatcher)
	{
		Connection = connection;
	}

	#endregion

	#region Properties

	public SmartCardReader Connection { get; }

	#endregion

	#region Methods

	public override void Refresh()
	{
		RefreshUniqueId();
	}

	private void RefreshUniqueId()
	{
		var command = new GetDataCommand(GetDataCommand.GetDataDataType.Uid);
		UpdateUniqueId(Connection.Transmit(command).Data);
	}

	#endregion
}