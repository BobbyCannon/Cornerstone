#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Data.Mappings;

[ExcludeFromCodeCoverage]
public class AccountMap : EntityMappingConfiguration<AccountEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<AccountEntity> b)
	{
		b.ToTable("Accounts", "dbo");
		b.HasKey(x => x.Id);

		//
		// All database names are going to be renamed to ensure all
		//  - BULK commands work
		//  - Custom SQL works
		//

		b.Property(x => x.AddressId).HasColumnName("AccountAddressId").IsRequired(false);
		b.Property(x => x.AddressSyncId).HasColumnName("AccountAddressSyncId").IsRequired(false);
		b.Property(x => x.CreatedOn).HasColumnName("AccountCreatedOn").IsRequired();
		b.Property(x => x.EmailAddress).HasColumnName("AccountEmailAddress").IsRequired(false);
		b.Property(x => x.ExternalId).HasColumnName("AccountExternalId").IsRequired(false);
		b.Property(x => x.Id).HasColumnName("AccountId").IsRequired();
		b.Property(x => x.IsDeleted).HasColumnName("AccountIsDeleted").IsRequired();
		b.Property(x => x.LastLoginDate).HasColumnName("AccountLastLoginDate").IsRequired();
		b.Property(x => x.ModifiedOn).HasColumnName("AccountModifiedOn").IsRequired();
		b.Property(x => x.Name).HasColumnName("AccountName").HasMaxLength(256).IsRequired();
		b.Property(x => x.Nickname).HasColumnName("AccountNickname").HasMaxLength(256).IsRequired(false);
		b.Property(x => x.PasswordHash).HasColumnName("AccountPasswordHash").IsRequired(false);
		b.Property(x => x.Roles).HasColumnName("AccountRoles").IsRequired(false);
		b.Property(x => x.SyncId).HasColumnName("AccountSyncId").IsRequired();

		b.HasIndex(x => x.AddressId).HasIndexName("IX_Accounts_AddressId");
		b.HasIndex(x => new { x.AddressId, x.ExternalId }).HasIndexName("IX_Accounts_AddressId_ExternalId").IsUnique();
		b.HasIndex(x => x.Name).HasIndexName("IX_Accounts_Name").IsUnique();
		b.HasIndex(x => x.Nickname).HasIndexName("IX_Accounts_Nickname").IsUnique();
		b.HasIndex(x => x.SyncId).HasIndexName("IX_Accounts_SyncId").IsUnique();

		b.HasOne(x => x.Address)
			.WithMany(x => x.Accounts)
			.HasForeignKey(x => x.AddressId)
			.OnDelete(DeleteBehavior.Restrict);
	}

	#endregion
}