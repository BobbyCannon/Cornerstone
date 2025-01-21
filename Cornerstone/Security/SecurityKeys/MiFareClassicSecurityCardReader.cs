﻿#region References

using System;
using System.Diagnostics;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Security.SecurityKeys.Apdu.Commands;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public class MiFareClassicSecurityCardReader : SecurityCardReader
{
	#region Constants

	public const int BlocksPerSector = 4;
	public const int BytesPerBlock = 16;
	public const string CardId = "3B-8F-80-01-80-4F-0C-A0-00-00-03-06-03-00-01-00-00-00-00-6A";
	public const int Sectors = 16;
	public const int TotalBlocks = Sectors * BlocksPerSector;
	public const int TotalSize = TotalBlocks * BytesPerBlock;

	#endregion

	#region Fields

	public static readonly byte[] KeyA = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

	#endregion

	#region Constructors

	public MiFareClassicSecurityCardReader(SmartCardReader connection, IDispatcher dispatcher)
		: base(connection, TotalSize, dispatcher)
	{
	}

	#endregion

	#region Methods

	public override void Refresh()
	{
		base.Refresh();

		LoadKeys();

		// MIFARE Classic 1K has 16 sectors, each with 4 blocks
		// Total Blocks = 16 x 4 = 64
		// Total Size = 64 x 16 = 1024 bytes

		for (byte block = 0; block < TotalBlocks; block++)
		{
			try
			{
				var authenticeCommand = new AuthenticateCommand(block);
				var response = Connection.Transmit(authenticeCommand);
				if (response.Status != 0x9000)
				{
					Connection.OnWriteLine($"Block {block} authenticated failed.");
					return;
				}

				Connection.OnWriteLine($"Block {block} authenticated successfully.");

				var readBinaryCommand = new ReadBinaryCommand(block, 16);
				response = Connection.Transmit(readBinaryCommand);
				if (response.Status != 0x9000)
				{
					Connection.OnWriteLine($"Failed to read the {block} block.");
					return;
				}

				Buffer.BlockCopy(
					response.Data, 0,
					Data, block * BytesPerBlock,
					Math.Min(response.Data.Length, BytesPerBlock)
				);

				//Debug.WriteLine(response.Data.ToHexString());
			}
			catch (Exception ex)
			{
				Connection.OnWriteLine(ex.ToDetailedString());
				break;
			}
		}
	}

	private void LoadKeys()
	{
		var command = new LoadKeysCommand(KeyA);
		var result = Connection.Transmit(command);
	}

	#endregion
}