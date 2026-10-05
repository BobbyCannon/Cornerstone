#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Plist;

/// <summary>
/// Represents an array value in a binary plist.
/// </summary>
[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "The spelling is correct.")]
internal class BinaryPlistArray
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the BinaryPlistArray class.
	/// </summary>
	/// <param name="objectTable"> A reference to the binary plist's object table. </param>
	public BinaryPlistArray(IList<BinaryPlistItem> objectTable)
		: this(objectTable, 0)
	{
	}

	/// <summary>
	/// Initializes a new instance of the BinaryPlistArray class.
	/// </summary>
	/// <param name="objectTable"> A reference to the binary plist's object table. </param>
	/// <param name="size"> The size of the array. </param>
	public BinaryPlistArray(IList<BinaryPlistItem> objectTable, int size)
	{
		ObjectReference = new List<int>(size);
		ObjectTable = objectTable;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the array's object reference collection.
	/// </summary>
	public IList<int> ObjectReference { get; }

	/// <summary>
	/// Gets a reference to the binary plist's object table.
	/// </summary>
	public IList<BinaryPlistItem> ObjectTable { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Converts this instance into an <see cref="T:object[]" /> array.
	/// </summary>
	/// <returns> The <see cref="T:object[]" /> array representation of this instance. </returns>
	public object[] ToArray()
	{
		var array = new object[ObjectReference.Count];
		int objectRef;
		object objectValue;
		BinaryPlistArray innerArray;
		BinaryPlistDictionary innerDict;

		for (var i = 0; i < array.Length; i++)
		{
			objectRef = ObjectReference[i];

			if ((objectRef >= 0) && (objectRef < ObjectTable.Count) && ((ObjectTable[objectRef] == null) || (ObjectTable[objectRef].Value != this)))
			{
				objectValue = ObjectTable[objectRef] == null ? null : ObjectTable[objectRef].Value;
				innerDict = objectValue as BinaryPlistDictionary;

				if (innerDict != null)
				{
					objectValue = innerDict.ToDictionary();
				}
				else
				{
					innerArray = objectValue as BinaryPlistArray;

					if (innerArray != null)
					{
						objectValue = innerArray.ToArray();
					}
				}

				array[i] = objectValue;
			}
		}

		return array;
	}

	/// <summary>
	/// Returns the string representation of this instance.
	/// </summary>
	/// <returns> This instance's string representation. </returns>
	public override string ToString()
	{
		var sb = new StringBuilder("[");
		int objectRef;

		for (var i = 0; i < ObjectReference.Count; i++)
		{
			if (i > 0)
			{
				sb.Append(",");
			}

			objectRef = ObjectReference[i];

			if ((ObjectTable.Count > objectRef) && ((ObjectTable[objectRef] == null) || (ObjectTable[objectRef].Value != this)))
			{
				sb.Append(ObjectReference[objectRef]);
			}
			else
			{
				sb.Append("*" + objectRef);
			}
		}

		return sb + "]";
	}

	#endregion
}