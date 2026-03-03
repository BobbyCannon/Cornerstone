#region References

using System.Text;
using Cornerstone.Text;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Text;

public class StringBuilderPoolTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Rent()
	{
		StringBuilderPool.Reset();

		using (var rented = StringBuilderPool.Rent())
		{
			var builder = rented.Value;
			AreEqual(0, builder.Length);
			AreEqual(StringBuilderPool.DefaultCapacity, builder.Capacity);
		}

		using (var rented = StringBuilderPool.Rent(256))
		{
			var builder = rented.Value;
			AreEqual(0, builder.Length);
			AreEqual(256, builder.Capacity);
		}
	}

	[Test]
	public void RentShouldResetCapacity()
	{
		StringBuilder builder;
		using (var rented = StringBuilderPool.Rent())
		{
			builder = rented.Value;
			AreEqual(0, builder.Length);
			builder.Append(new string(' ', StringBuilderPool.MaximumCapacity + 1));
			AreEqual(32769, builder.Capacity);
		}
		AreEqual(StringBuilderPool.DefaultCapacity, builder.Capacity);
	}

	[Test]
	public void Return()
	{
		StringBuilderPool.Return(null);
	}

	#endregion
}