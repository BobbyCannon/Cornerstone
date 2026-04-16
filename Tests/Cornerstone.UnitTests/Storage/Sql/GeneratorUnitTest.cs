#region References

using System;
using System.Collections.Generic;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

public class GeneratorUnitTest : CornerstoneUnitTest
{
	#region Methods

	protected void ClearGeneratedScripts()
	{
		var info = SourceReflector.GetSourceType(typeof(SqlGenerator));
		var tableScripts = (Dictionary<(Type, SqlProvider), string>) info.GetField("_tableScripts").GetValue(null);
		tableScripts.Clear();

		var upsertParameters = (Dictionary<(Type, SqlProvider), Func<object, IDictionary<string, (object, Type)>>>) info.GetField("_upsertParameters").GetValue(null);
		upsertParameters.Clear();

		var upsertScripts = (Dictionary<(Type, SqlProvider), string>) info.GetField("_upsertScripts").GetValue(null);
		upsertScripts.Clear();
	}

	#endregion
}