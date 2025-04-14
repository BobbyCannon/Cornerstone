#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Sync;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncModelTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void UpdateScenarios()
	{
		if (!EnableFileUpdates && !IsDebugging)
		{
			// Skip this test
			return;
		}

		var filePath = $@"{SolutionDirectory}\Tests\Cornerstone.UnitTests\Sync\{nameof(SyncModelTests)}.cs";
		var entityTypes = FindModels();
		var builder = new TextBuilder();
		var endIndex = entityTypes.Length - 1;
		var actionTypes = EnumExtensions.GetEnumValues(UpdateableAction.None, UpdateableAction.All, UpdateableAction.SyncAll);

		for (var i = 0; i <= endIndex; i++)
		{
			var entityType = entityTypes[i];

			// Must create the entity to ensure the Cache is updated
			entityType.CreateInstance();

			builder.AppendLineThenPushIndent("{");
			builder.AppendLine($"typeof({entityType.Name}),");
			builder.AppendLine("new Dictionary<UpdateableAction, IncludeExcludeSettings>");
			builder.AppendLineThenPushIndent("{");

			foreach (var actionType in actionTypes)
			{
				var includeExcludeSettings = entityType.GetIncludeExcludeSettings(actionType);

				builder.AppendLineThenPushIndent("{");
				builder.AppendLine($"UpdateableAction.{actionType},");
				builder.AppendLineThenPopIndent($"{includeExcludeSettings.ToCSharp()}");
				builder.AppendLine("},");
			}

			builder.PopIndent();
			builder.AppendLineThenPopIndent("}");
			builder.AppendLine("},");
		}

		FileModifier.UpdateFileIfNecessary("// <Scenarios>\r\n", "// </Scenarios>", filePath, builder.ToString());
	}

	[TestMethod]
	public void ValidateUpdateableProperties()
	{
		var entities = FindModels();
		var scenarios = GetExpectedValues();

		foreach (var entityType in entities)
		{
			var scenario = scenarios[entityType];
			var entity = (IEntity) entityType.CreateInstance();

			foreach (var action in scenario.Keys)
			{
				var actual = entity.GetIncludeExcludeSettings(action);
				var expected = scenario[action];
				AreEqual(expected, actual, () => entityType.FullName);
			}
		}
	}

	private Type[] FindModels()
	{
		var assembly = typeof(IServerDatabase).Assembly;
		var entityTypes = assembly.GetTypes()
			.Where(x => x.BaseType == typeof(SyncModel))
			.ToArray();

		return entityTypes;
	}

	private Dictionary<Type, Dictionary<UpdateableAction, IncludeExcludeSettings>> GetExpectedValues()
	{
		var expected = new Dictionary<Type, Dictionary<UpdateableAction, IncludeExcludeSettings>>
		{
			// <Scenarios>
			{
				typeof(Account),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(Address),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(LogEvent),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(Setting),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					}
				}
			}
			// </Scenarios>
		};

		return expected;
	}

	#endregion
}