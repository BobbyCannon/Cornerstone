#region References

using Cornerstone.Sample.Models;
using Cornerstone.Storage.Sql.Migrations;

#endregion

namespace Cornerstone.Sample.Storage.Migrations;

public class Initial : SqlMigration
{
	#region Properties

	public override string Id => "20260914120000_Initial";

	#endregion

	#region Methods

	public override void Down(MigrationBuilder builder)
	{
		builder.DropTable("Accounts");
		builder.DropTable("Bookmarks");
		builder.DropTable("Settings");
		builder.DropTable("Addresses");
		builder.DropTable("Customers");
	}

	public override void Up(MigrationBuilder builder)
	{
		builder.CreateTable(typeof(CustomerEntity));
		builder.CreateTable(typeof(AddressEntity));
		builder.CreateTable(typeof(AccountEntity));
		builder.CreateTable(typeof(BookmarkEntity));
		builder.CreateTable(typeof(SettingEntity));
		builder.CreateIndex("Customers", SchemaSnapshot.CreateIndex("IX_Customers_SyncId", true, "SyncId"));
		builder.CreateIndex("Customers", SchemaSnapshot.CreateIndex("IX_Customers_ModifiedOn", false, "ModifiedOn"));
		builder.CreateIndex("Addresses", SchemaSnapshot.CreateIndex("IX_Addresses_SyncId", true, "SyncId"));
		builder.CreateIndex("Addresses", SchemaSnapshot.CreateIndex("IX_Addresses_ModifiedOn", false, "ModifiedOn"));
		builder.CreateIndex("Accounts", SchemaSnapshot.CreateIndex("IX_Accounts_LastLoginDate", false, "LastLoginDate"));
		builder.CreateIndex("Accounts", SchemaSnapshot.CreateIndex("IX_Accounts_SyncId", true, "SyncId"));
		builder.CreateIndex("Accounts", SchemaSnapshot.CreateIndex("IX_Accounts_ModifiedOn", false, "ModifiedOn"));
		builder.CreateIndex("Bookmarks", SchemaSnapshot.CreateIndex("IX_Bookmarks_SyncId", true, "SyncId"));
		builder.CreateIndex("Bookmarks", SchemaSnapshot.CreateIndex("IX_Bookmarks_ModifiedOn", false, "ModifiedOn"));
		builder.CreateIndex("Settings", SchemaSnapshot.CreateIndex("IX_Settings_SyncId", true, "SyncId"));
		builder.CreateIndex("Settings", SchemaSnapshot.CreateIndex("IX_Settings_ModifiedOn", false, "ModifiedOn"));
	}

	#endregion
}
