#region References

using System;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public class SecurityCard : Bindable
{
	#region Constructors

	public SecurityCard(int totalSize, IDispatcher dispatcher) : base(dispatcher)
	{
		Data = new byte[totalSize];
	}

	#endregion

	#region Properties

	public byte[] Data { get; }

	public string UniqueId { get; private set; }

	#endregion

	#region Methods

	public virtual void Refresh()
	{
	}

	public void UpdateUniqueId(byte[] values)
	{
		UniqueId = BitConverter.ToString(values);
	}

	#endregion
}