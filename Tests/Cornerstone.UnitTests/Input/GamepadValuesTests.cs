#region References

using Cornerstone.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Input;

[TestClass]
public class GamepadValuesTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AxisPastHalfUsesTheUpperHalfOfTheRange()
	{
		IsFalse(GamepadValues.AxisPastHalf(127, 0, 255));
		IsTrue(GamepadValues.AxisPastHalf(128, 0, 255));
		IsFalse(GamepadValues.AxisPastHalf(0, 5, 5));
	}

	[TestMethod]
	public void HatDirectionUsesSignAroundCenter()
	{
		AreEqual(0, GamepadValues.HatDirection(0, -1, 1, 0));
		AreEqual(-1, GamepadValues.HatDirection(-1, -1, 1, 0));
		AreEqual(1, GamepadValues.HatDirection(1, -1, 1, 0));
	}

	[TestMethod]
	public void ToThumbFromRangeInvertsVerticalAxis()
	{
		AreEqual((short) -32768, GamepadValues.ToThumbFromRange(-32768, -32768, 32767, false));
		AreEqual((short) 32767, GamepadValues.ToThumbFromRange(32767, -32768, 32767, false));
		AreEqual((short) 0, GamepadValues.ToThumbFromRange(0, -32768, 32767, false));
		AreEqual((short) 32767, GamepadValues.ToThumbFromRange(-32768, -32768, 32767, true));
		AreEqual((short) -32768, GamepadValues.ToThumbFromRange(32767, -32768, 32767, true));
	}

	[TestMethod]
	public void ToThumbMapsFullScale()
	{
		AreEqual((short) 0, GamepadValues.ToThumb(0d));
		AreEqual((short) 0, GamepadValues.ToThumb(double.NaN));
		AreEqual((short) 32767, GamepadValues.ToThumb(1d));
		AreEqual((short) 32767, GamepadValues.ToThumb(2d));
		AreEqual((short) -32768, GamepadValues.ToThumb(-1d));
		AreEqual((short) -32768, GamepadValues.ToThumb(-2d));
	}

	[TestMethod]
	public void ToTriggerFromRangeMapsRestToFull()
	{
		AreEqual((byte) 0, GamepadValues.ToTriggerFromRange(0, 0, 255));
		AreEqual((byte) 255, GamepadValues.ToTriggerFromRange(255, 0, 255));
		AreEqual((byte) 0, GamepadValues.ToTriggerFromRange(10, 4, 4));
	}

	[TestMethod]
	public void ToTriggerMapsFullScale()
	{
		AreEqual((byte) 0, GamepadValues.ToTrigger(0d));
		AreEqual((byte) 0, GamepadValues.ToTrigger(double.NaN));
		AreEqual((byte) 0, GamepadValues.ToTrigger(-1d));
		AreEqual((byte) 255, GamepadValues.ToTrigger(1d));
		AreEqual((byte) 255, GamepadValues.ToTrigger(2d));
	}

	#endregion
}
