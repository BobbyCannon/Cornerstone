#region References

using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Security.SecurityKeys.Apdu;
using Cornerstone.Security.SecurityKeys.Apdu.Commands;
using System;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public abstract class ApduSecurityCardReader : SecurityCardReader
{
	#region Constructors

	protected ApduSecurityCardReader(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	public override void ReadCard()
	{
		var card = Card;
		switch (card)
		{
			case MiFareClassicSecurityCard:
			{
				var command = new LoadKeysCommand(MiFareClassicSecurityCard.KeyA);
				var result = Transmit(command);
				if (result.Status != 0x9000)
				{
					return;
				}

				RefreshMiFareClassic(MaximumDataToRead);
				break;
			}
			case MiFareUltralightSecurityCard:
			{
				RefreshMiFareUltraLight(MaximumDataToRead);
				break;
			}
		}
	}

	public override void StartListening()
	{
	}

	public override void StopListening()
	{
	}

	public byte[] WriteBlock(byte block, byte[] data)
	{
		if (!Card.IsUnsafeBlock(block)
			&& !Card.EnableUnsafeWrite)
		{
			return data;
		}

		var authenticeCommand = new AuthenticateCommand(block);
		var response = Transmit(authenticeCommand);
		if (response.Status != 0x9000)
		{
			return null;
		}

		var command = new WriteBytesCommand(block, data);
		response = Transmit(command);
		if (response.Status != 0x9000)
		{
			return null;
		}

		return response.Data;
	}

	public override void WriteCard()
	{
		var card = Card;
		switch (card)
		{
			case MiFareClassicSecurityCard:
			{
				// Block 0 is read only, block 3 is the Sector's end
				WriteBlock(1, card.Data.SubArray(16, 16));
				WriteBlock(2, card.Data.SubArray(32, 16));

				if (card.EnableUnsafeWrite)
				{
					WriteBlock(3, card.Data.SubArray(48, 16));
				}

				WriteBlock(4, card.Data.SubArray(64, 16));
				WriteBlock(5, card.Data.SubArray(80, 16));
				WriteBlock(6, card.Data.SubArray(96, 16));

				if (card.EnableUnsafeWrite)
				{
					WriteBlock(7, card.Data.SubArray(112, 16));
				}
				break;
			}
			case MiFareUltralightSecurityCard:
			{
				break;
			}
		}
	}

	protected abstract byte[] Transmit(byte[] command);

	private byte[] ReadBlock(byte block)
	{
		var authenticeCommand = new AuthenticateCommand(block);
		var response = Transmit(authenticeCommand);
		if (response.Status != 0x9000)
		{
			return null;
		}

		var readBinaryCommand = new ReadBytesCommand(block, 16);
		response = Transmit(readBinaryCommand);
		if (response.Status != 0x9000)
		{
			return null;
		}

		return response.Data;
	}

	private void RefreshMiFareClassic(int maxLength)
	{
		// MIFARE Classic 1K has 16 sectors, each with 4 blocks
		// Total Blocks = 16 x 4 = 64
		// Total Size = 64 x 16 = 1024 bytes
		var read = 0;
		var length = Math.Min(MiFareClassicSecurityCard.TotalSize, maxLength);

		for (byte block = 0; block < MiFareClassicSecurityCard.TotalBlocks; block++)
		{
			try
			{
				var data = ReadBlock(block);
				if (data == null)
				{
					break;
				}

				Card.WriteData(block, data);

				read += MiFareClassicSecurityCard.BytesPerBlock;

				if (read >= length)
				{
					break;
				}
			}
			catch (Exception)
			{
				//OnWriteLine(ex.ToDetailedString());
				break;
			}
		}
	}

	private void RefreshMiFareUltraLight(int maxLength)
	{
		if (Card is not MiFareUltralightSecurityCard card)
		{
			return;
		}

		var read = 0;
		var length = Math.Min(MiFareUltralightSecurityCard.TotalSize, maxLength);

		for (byte page = 0; page < MiFareUltralightSecurityCard.TotalPages; page++)
		{
			try
			{
				var readBinaryCommand = new ReadBytesCommand(page, 4);
				var response = Transmit(readBinaryCommand);
				if (response.Status != 0x9000)
				{
					break;
				}

				card.WriteData(page, response.Data);
				read += response.Data.Length;

				if (read >= length)
				{
					break;
				}
			}
			catch (Exception)
			{
				//OnWriteLine(ex.ToDetailedString());
				break;
			}
		}
	}

	private ApduResponse Transmit(ApduCommand command)
	{
		var apduBuffer = command.ToByteArray();
		var response = Transmit(apduBuffer);
		return new ApduResponse(response);
	}

	#endregion
}