#region References

using Cornerstone.EntityFramework;
using Cornerstone.Sample.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#endregion

namespace Cornerstone.Sample.Storage.Mappings;

public class AccountEntityMap : EntityMappingConfiguration<AccountEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<AccountEntity> builder)
	{
		builder.ToTable("Accounts");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.CreatedOn).IsRequired();
		builder.Property(x => x.EmailAddress).IsRequired();
		builder.Property(x => x.Id).IsRequired();
		builder.Property(x => x.IsDeleted).IsRequired();
		builder.Property(x => x.ModifiedOn).IsRequired();
		builder.Property(x => x.Name).IsRequired();
		builder.Property(x => x.Roles).IsRequired();
		builder.Property(x => x.SyncId).IsRequired();

		builder.HasIndex(x => x.SyncId)
			.HasIndexName("IX_Accounts_SyncId")
			.IsUnique();
		builder.HasIndex(x => x.ModifiedOn)
			.HasIndexName("IX_Accounts_ModifiedOn");

		builder.HasOne<AddressEntity>()
			.WithMany()
			.HasForeignKey(x => x.AddressId)
			.IsRequired(false);

		builder.HasOne<CustomerEntity>()
			.WithMany()
			.HasForeignKey(x => x.CustomerId)
			.IsRequired(false);
	}

	#endregion
}