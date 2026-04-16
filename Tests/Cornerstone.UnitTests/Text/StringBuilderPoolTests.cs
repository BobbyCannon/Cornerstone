#region References

using System.Text;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class StringBuilderPoolTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
	public void Return()
	{
		StringBuilderPool.Return(null);
	}

	#endregion
}