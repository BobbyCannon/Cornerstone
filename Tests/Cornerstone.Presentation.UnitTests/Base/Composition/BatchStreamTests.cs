#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Rendering.Composition.Transport;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Composition;

[TestClass]
public class BatchStreamTests
{
	#region Methods

	[PresentationTestMethod]
	public void BatchStreamCorrectlyWritesAndReadsData()
	{
		var data = new BatchStreamData();
		var memPool = new BatchStreamMemoryPool(false, 100, _ => { });
		var objPool = new BatchStreamObjectPool<object>(false, 10, _ => { });

		var guids = new List<Guid>();
		var objects = new List<object>();
		for (var c = 0; c < 453; c++)
		{
			guids.Add(Guid.NewGuid());
			objects.Add(new object());
		}

		using (var writer = new BatchStreamWriter(data, memPool, objPool))
		{
			foreach (var guid in guids)
			{
				writer.Write(guid);
			}
			foreach (var obj in objects)
			{
				writer.WriteObject(obj);
			}
		}

		using (var reader = new BatchStreamReader(data, memPool, objPool))
		{
			foreach (var guid in guids)
			{
				CornerstoneTest.AreEqual(guid, reader.Read<Guid>());
			}
			foreach (var obj in objects)
			{
				CornerstoneTest.AreEqual(obj, reader.ReadObject());
			}
		}
	}

	#endregion
}