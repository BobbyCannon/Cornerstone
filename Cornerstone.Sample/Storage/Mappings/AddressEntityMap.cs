#region References

using Cornerstone.EntityFramework;
using Cornerstone.Sample.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#endregion

namespace Cornerstone.Sample.Storage.Mappings;

public class AddressEntityMap : EntityMappingConfiguration<AddressEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<AddressEntity> builder)
	{
		builder.ToTable("Addresses");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.CreatedOn).IsRequired();
		builder.Property(x => x.Id).IsRequired();
		builder.Property(x => x.IsDeleted).IsRequired();
		builder.Property(x => x.Line1).IsRequired();
		builder.Property(x => x.ModifiedOn).IsRequired();
		builder.Property(x => x.SyncId).IsRequired();

		builder.HasIndex(x => x.SyncId)
			.HasIndexName("IX_Addresses_SyncId")
			.IsUnique();
		builder.HasIndex(x => x.ModifiedOn)
			.HasIndexName("IX_Addresses_ModifiedOn");

		builder.HasOne<CustomerEntity>()
			.WithMany()
			.HasForeignKey(x => x.CustomerId)
			.IsRequired(false);
	}

	#endregion
}