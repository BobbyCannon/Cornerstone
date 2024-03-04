#region References

#endregion

namespace Cornerstone.Collections;

internal static class GapBufferExtensions
{
	#region Methods

	internal static void Insert(this GapBuffer<char> buffer, int index, string array, int arrayIndex, int arrayCount)
	{
		for (var i = arrayIndex; i < (arrayIndex + arrayCount); i++)
		{
			buffer.Insert(index, array[arrayIndex]);
		}
	}

	#endregion
}