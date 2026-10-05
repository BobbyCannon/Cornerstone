#region References

using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

public class CursorFactoryMock : ICursorFactory
{
	#region Methods

	public ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot)
	{
		return new MockCursorImpl();
	}

	public ICursorImpl GetCursor(StandardCursorType cursorType)
	{
		return new MockCursorImpl();
	}

	#endregion

	#region Classes

	private class MockCursorImpl : ICursorImpl
	{
		#region Methods

		public void Dispose()
		{
		}

		#endregion
	}

	#endregion
}