#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public partial class SecurityCard : CornerstoneObject, IDispatchable
{
	#region Fields

	private readonly IDispatcher _dispatcher;

	#endregion

	#region Constructors

	public SecurityCard(int totalSize, IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
		Data = new byte[totalSize];
	}

	#endregion

	#region Properties

	public byte[] Data { get; }

	[Notify]
	public partial bool EnableUnsafeWrite { get; set; }

	[Notify]
	public partial string UniqueId { get; private set; }

	#endregion

	#region Methods

	public virtual void ClearData()
	{
		if (Data == null)
		{
			return;
		}

		for (var i = 0; i < Data.Length; i++)
		{
			Data[i] = 0;
		}
	}

	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public virtual bool IsUnsafeBlock(byte block)
	{
		return false;
	}

	public override string ToString()
	{
		return GetType().Name.ToSentenceCase();
	}

	public void UpdateUniqueId(byte[] values)
	{
		UniqueId = BitConverter.ToString(values);
	}

	public byte[] WriteData(byte[] data)
	{
		var blocks = data.Length / 16;
		Array.Copy(data, 0, Data, 0, blocks * 16);
		return data;
	}

	public virtual byte[] WriteData(byte block, byte[] data)
	{
		Array.Copy(data, 0, Data, block * 16, Math.Min(16, data.Length));
		return data;
	}

	#endregion
}
