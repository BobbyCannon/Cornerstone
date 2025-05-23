﻿#region References

using System.Collections.Generic;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil.Rocks;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Methods

	public void ProcessTypes()
	{
		ProcessTypes(NotifyNodes);
	}

	private void ProcessTypes(List<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes)
		{
			if (node.EventInvoker == null)
			{
				continue;
			}

			ModuleWeaver.WriteDebug("\t" + node.TypeDefinition.FullName);

			foreach (var propertyData in node.PropertyData)
			{
				var body = propertyData.PropertyDefinition.SetMethod.Body;

				var alreadyHasEquality = HasEqualityChecker.AlreadyHasEquality(propertyData.PropertyDefinition, propertyData.BackingFieldReference);

				body.SimplifyMacros();

				body.MakeLastStatementReturn();

				var propertyWeaver = new PropertyWeaver(this, propertyData, node, ModuleWeaver.TypeSystem);
				propertyWeaver.Execute();

				if (!alreadyHasEquality)
				{
					var equalityCheckWeaver = new EqualityCheckWeaver(propertyData, node.TypeDefinition, this);
					equalityCheckWeaver.Execute();
				}

				body.InitLocals = true;
				body.OptimizeMacros();
			}

			ProcessTypes(node.Nodes);
		}
	}

	#endregion
}