#region References

#endregion

namespace Cornerstone.FileSystem.Sync;

public static class Utilities
{
	#region Methods

	public static string Pluralize(string noun, int count)
	{
		if (count == 1)
		{
			return noun;
		}
		return noun + "s";
	}

	#endregion
}