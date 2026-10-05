#region References

using Cornerstone.EntityFramework;
using Cornerstone.Sample.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#endregion

namespace Cornerstone.Sample.Storage.Mappings;

public class CustomerEntityMap : EntityMappingConfiguration<CustomerEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<CustomerEntity> builder)
	{
		builder.ToTable("Customers");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.CreatedOn).IsRequired();
		builder.Property(x => x.Id).IsRequired();
		builder.Property(x => x.IsDeleted).IsRequired();
		builder.Property(x => x.ModifiedOn).IsRequired();
		builder.Property(x => x.Name).IsRequired();
		builder.Property(x => x.SyncId).IsRequired();

		builder.HasIndex(x => x.SyncId)
			.HasIndexName("IX_Customers_SyncId")
			.IsUnique();
		builder.HasIndex(x => x.ModifiedOn)
			.HasIndexName("IX_Customers_ModifiedOn");
	}

	#endregion
}
