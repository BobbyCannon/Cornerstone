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
using Sample.Shared.Storage.Client;
using Sample.Shared.Storage.Server;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class EntityTests : CornerstoneUnitTest
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

		var filePath = $@"{SolutionDirectory}\Tests\Cornerstone.UnitTests\Sync\{nameof(EntityTests)}.cs";
		var entityTypes = FindEntities();
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
		var entities = FindEntities();
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

	private Type[] FindEntities()
	{
		var assembly = typeof(IServerDatabase).Assembly;
		var entityTypes = assembly.GetTypes()
			.Where(x =>
				x.BaseType.ImplementsType(typeof(Entity<>))
				|| x.BaseType.ImplementsType(typeof(SyncEntity<>))
			)
			.ToArray();

		return entityTypes;
	}

	private Dictionary<Type, Dictionary<UpdateableAction, IncludeExcludeSettings>> GetExpectedValues()
	{
		var expected = new Dictionary<Type, Dictionary<UpdateableAction, IncludeExcludeSettings>>
		{
			// <Scenarios>
			{
				typeof(AccountEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "AddressSyncId", "EmailAddress", "Name", "Roles"],
							["Address", "AddressId", "ExternalId", "Groups", "Id", "LastLoginDate", "Nickname", "PasswordHash", "Pets"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "AddressSyncId", "EmailAddress", "Name", "Roles", "SyncId"],
							["Address", "AddressId", "ExternalId", "Groups", "Id", "LastLoginDate", "Nickname", "PasswordHash", "Pets"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "AddressSyncId", "EmailAddress", "Name", "Roles"],
							["Address", "AddressId", "ExternalId", "Groups", "Id", "LastLoginDate", "Nickname", "PasswordHash", "Pets"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["AddressId", "AddressSyncId", "CreatedOn", "EmailAddress", "ExternalId", "Id", "IsDeleted", "LastLoginDate", "ModifiedOn", "Name", "Nickname", "PasswordHash", "Roles", "SyncId"],
							["Address", "Groups", "Pets"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["Address", "AddressId", "AddressSyncId", "CreatedOn", "EmailAddress", "ExternalId", "Groups", "Id", "IsDeleted", "LastLoginDate", "ModifiedOn", "Name", "Nickname", "PasswordHash", "Pets", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["Address", "AddressId", "AddressSyncId", "CreatedOn", "EmailAddress", "ExternalId", "Groups", "Id", "IsDeleted", "LastLoginDate", "ModifiedOn", "Name", "Nickname", "PasswordHash", "Pets", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["Address", "AddressId", "AddressSyncId", "CreatedOn", "EmailAddress", "ExternalId", "Groups", "Id", "IsDeleted", "LastLoginDate", "ModifiedOn", "Name", "Nickname", "PasswordHash", "Pets", "Roles", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(AddressEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "City", "Line1", "Line2", "Postal", "State"],
							["Account", "AccountId", "Accounts", "AccountSyncId", "FullAddress", "Id", "LinkedAddress", "LinkedAddresses", "LinkedAddressId", "LinkedAddressSyncId"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "City", "Line1", "Line2", "Postal", "State", "SyncId"],
							["Account", "AccountId", "Accounts", "AccountSyncId", "FullAddress", "Id", "LinkedAddress", "LinkedAddresses", "LinkedAddressId", "LinkedAddressSyncId"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "City", "Line1", "Line2", "Postal", "State"],
							["Account", "AccountId", "Accounts", "AccountSyncId", "FullAddress", "Id", "LinkedAddress", "LinkedAddresses", "LinkedAddressId", "LinkedAddressSyncId"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["AccountId", "AccountSyncId", "City", "CreatedOn", "FullAddress", "Id", "IsDeleted", "Line1", "Line2", "LinkedAddressId", "LinkedAddressSyncId", "ModifiedOn", "Postal", "State", "SyncId"],
							["Account", "Accounts", "LinkedAddress", "LinkedAddresses"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["Account", "AccountId", "Accounts", "AccountSyncId", "City", "CreatedOn", "FullAddress", "Id", "IsDeleted", "Line1", "Line2", "LinkedAddress", "LinkedAddresses", "LinkedAddressId", "LinkedAddressSyncId", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["Account", "AccountId", "Accounts", "AccountSyncId", "City", "CreatedOn", "FullAddress", "Id", "IsDeleted", "Line1", "Line2", "LinkedAddress", "LinkedAddresses", "LinkedAddressId", "LinkedAddressSyncId", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["Account", "AccountId", "Accounts", "AccountSyncId", "City", "CreatedOn", "FullAddress", "Id", "IsDeleted", "Line1", "Line2", "LinkedAddress", "LinkedAddresses", "LinkedAddressId", "LinkedAddressSyncId", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(FoodEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							[],
							["ChildRelationships", "CreatedOn", "Id", "ModifiedOn", "Name", "ParentRelationships"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							[],
							["ChildRelationships", "CreatedOn", "Id", "ModifiedOn", "Name", "ParentRelationships"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							[],
							["ChildRelationships", "CreatedOn", "Id", "ModifiedOn", "Name", "ParentRelationships"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "ModifiedOn", "Name"],
							["ChildRelationships", "ParentRelationships"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["ChildRelationships", "CreatedOn", "Id", "ModifiedOn", "Name", "ParentRelationships"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["ChildRelationships", "CreatedOn", "Id", "ModifiedOn", "Name", "ParentRelationships"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["ChildRelationships", "CreatedOn", "Id", "ModifiedOn", "Name", "ParentRelationships"],
							[]
						)
					}
				}
			},
			{
				typeof(FoodRelationshipEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							[],
							["Child", "ChildId", "CreatedOn", "Id", "ModifiedOn", "Parent", "ParentId", "Quantity"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							[],
							["Child", "ChildId", "CreatedOn", "Id", "ModifiedOn", "Parent", "ParentId", "Quantity"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							[],
							["Child", "ChildId", "CreatedOn", "Id", "ModifiedOn", "Parent", "ParentId", "Quantity"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["ChildId", "CreatedOn", "Id", "ModifiedOn", "ParentId", "Quantity"],
							["Child", "Parent"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["Child", "ChildId", "CreatedOn", "Id", "ModifiedOn", "Parent", "ParentId", "Quantity"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["Child", "ChildId", "CreatedOn", "Id", "ModifiedOn", "Parent", "ParentId", "Quantity"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["Child", "ChildId", "CreatedOn", "Id", "ModifiedOn", "Parent", "ParentId", "Quantity"],
							[]
						)
					}
				}
			},
			{
				typeof(GroupEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Description", "Id", "Members", "ModifiedOn", "Name"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Description", "Id", "Members", "ModifiedOn", "Name"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Description", "Id", "Members", "ModifiedOn", "Name"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "Description", "Id", "ModifiedOn", "Name"],
							["Members"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["CreatedOn", "Description", "Id", "Members", "ModifiedOn", "Name"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["CreatedOn", "Description", "Id", "Members", "ModifiedOn", "Name"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "Description", "Id", "Members", "ModifiedOn", "Name"],
							[]
						)
					}
				}
			},
			{
				typeof(GroupMemberEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Group", "GroupId", "Id", "Member", "MemberId", "MemberSyncId", "ModifiedOn", "Role"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Group", "GroupId", "Id", "Member", "MemberId", "MemberSyncId", "ModifiedOn", "Role"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Group", "GroupId", "Id", "Member", "MemberId", "MemberSyncId", "ModifiedOn", "Role"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "GroupId", "Id", "MemberId", "MemberSyncId", "ModifiedOn", "Role"],
							["Group", "Member"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["CreatedOn", "Group", "GroupId", "Id", "Member", "MemberId", "MemberSyncId", "ModifiedOn", "Role"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["CreatedOn", "Group", "GroupId", "Id", "Member", "MemberId", "MemberSyncId", "ModifiedOn", "Role"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "Group", "GroupId", "Id", "Member", "MemberId", "MemberSyncId", "ModifiedOn", "Role"],
							[]
						)
					}
				}
			},
			{
				typeof(LogEventEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "Level", "Message"],
							["AcknowledgedOn", "Id", "LoggedOn"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Level", "Message", "SyncId"],
							["AcknowledgedOn", "Id", "LoggedOn"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "Level", "Message"],
							["AcknowledgedOn", "Id", "LoggedOn"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["AcknowledgedOn", "CreatedOn", "Id", "IsDeleted", "Level", "LoggedOn", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["AcknowledgedOn", "CreatedOn", "Id", "IsDeleted", "Level", "LoggedOn", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["AcknowledgedOn", "CreatedOn", "Id", "IsDeleted", "Level", "LoggedOn", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["AcknowledgedOn", "CreatedOn", "Id", "IsDeleted", "Level", "LoggedOn", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(PetEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Id", "ModifiedOn", "Name", "Owner", "OwnerId", "Type", "TypeId"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Id", "ModifiedOn", "Name", "Owner", "OwnerId", "Type", "TypeId"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Id", "ModifiedOn", "Name", "Owner", "OwnerId", "Type", "TypeId"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "ModifiedOn", "Name", "OwnerId", "TypeId"],
							["Owner", "Type"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "ModifiedOn", "Name", "Owner", "OwnerId", "Type", "TypeId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "ModifiedOn", "Name", "Owner", "OwnerId", "Type", "TypeId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "ModifiedOn", "Name", "Owner", "OwnerId", "Type", "TypeId"],
							[]
						)
					}
				}
			},
			{
				typeof(PetTypeEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Id", "ModifiedOn", "Type", "Types"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Id", "ModifiedOn", "Type", "Types"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							[],
							["CreatedOn", "Id", "ModifiedOn", "Type", "Types"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "ModifiedOn", "Type"],
							["Types"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "ModifiedOn", "Type", "Types"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "ModifiedOn", "Type", "Types"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "ModifiedOn", "Type", "Types"],
							[]
						)
					}
				}
			},
			{
				typeof(SettingEntity),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["ServerName", "ServerValue", "CreatedOn", "IsDeleted", "ModifiedOn", "SyncId"],
							["Id"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["ServerValue", "CreatedOn", "IsDeleted", "ModifiedOn"],
							["Id", "ServerName", "SyncId"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["ServerName", "ServerValue", "CreatedOn", "IsDeleted", "ModifiedOn", "SyncId"],
							["Id"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "ModifiedOn", "ServerName", "ServerValue", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "ModifiedOn", "ServerName", "ServerValue", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "ModifiedOn", "ServerName", "ServerValue", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "ModifiedOn", "ServerName", "ServerValue", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(ClientAccount),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "AddressSyncId", "EmailAddress", "Name", "Roles"],
							["Address", "AddressId", "Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "AddressSyncId", "EmailAddress", "Name", "Roles", "SyncId"],
							["Address", "AddressId", "Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "AddressSyncId", "EmailAddress", "Name", "Roles"],
							["Address", "AddressId", "Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["AddressId", "AddressSyncId", "CreatedOn", "EmailAddress", "Id", "IsDeleted", "LastClientUpdate", "ModifiedOn", "Name", "Roles", "SyncId"],
							["Address"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["Address", "AddressId", "AddressSyncId", "CreatedOn", "EmailAddress", "Id", "IsDeleted", "LastClientUpdate", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["Address", "AddressId", "AddressSyncId", "CreatedOn", "EmailAddress", "Id", "IsDeleted", "LastClientUpdate", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["Address", "AddressId", "AddressSyncId", "CreatedOn", "EmailAddress", "Id", "IsDeleted", "LastClientUpdate", "ModifiedOn", "Name", "Roles", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(ClientAddress),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "City", "Line1", "Line2", "Postal", "State"],
							["Accounts", "Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "City", "Line1", "Line2", "Postal", "State", "SyncId"],
							["Accounts", "Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "City", "Line1", "Line2", "Postal", "State"],
							["Accounts", "Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["City", "CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							["Accounts"]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["Accounts", "City", "CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["Accounts", "City", "CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["Accounts", "City", "CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(ClientLogEvent),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "Level", "Message"],
							["Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Level", "Message", "SyncId"],
							["Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "Level", "Message"],
							["Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "Level", "Message", "ModifiedOn", "SyncId"],
							[]
						)
					}
				}
			},
			{
				typeof(ClientSetting),
				new Dictionary<UpdateableAction, IncludeExcludeSettings>
				{
					{
						UpdateableAction.SyncIncomingAdd,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "Name", "Value"],
							["Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.SyncIncomingUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"],
							["Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.SyncOutgoing,
						new IncludeExcludeSettings(
							["CreatedOn", "IsDeleted", "ModifiedOn", "SyncId", "Name", "Value"],
							["Id", "LastClientUpdate"]
						)
					},
					{
						UpdateableAction.UnwrapProxyEntity,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					},
					{
						UpdateableAction.Updateable,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					},
					{
						UpdateableAction.PropertyChangeTracking,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "ModifiedOn", "Name", "SyncId", "Value"],
							[]
						)
					},
					{
						UpdateableAction.PartialUpdate,
						new IncludeExcludeSettings(
							["CreatedOn", "Id", "IsDeleted", "LastClientUpdate", "ModifiedOn", "Name", "SyncId", "Value"],
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