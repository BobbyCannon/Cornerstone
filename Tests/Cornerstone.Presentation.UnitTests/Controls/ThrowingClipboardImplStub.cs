#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

internal sealed class ThrowingClipboardImplStub(Type exceptionType) : IClipboardImpl
{
	#region Properties

	public int SetDataCount { get; private set; }

	public int TryGetDataCount { get; private set; }

	#endregion

	#region Methods

	public Task ClearAsync()
	{
		return Task.FromException(CreateException());
	}

	public Task SetDataAsync(IAsyncDataTransfer dataTransfer)
	{
		++SetDataCount;
		return Task.FromException(CreateException());
	}

	public Task<IAsyncDataTransfer> TryGetDataAsync()
	{
		++TryGetDataCount;
		return Task.FromException<IAsyncDataTransfer>(CreateException());
	}

	private Exception CreateException()
	{
		return (Exception) Activator.CreateInstance(exceptionType)!;
	}

	#endregion
}