#region References

using Cornerstone.EntityFramework;
using Cornerstone.Sample.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#endregion

namespace Cornerstone.Sample.Storage.Mappings;

public class BookmarkEntityMap : EntityMappingConfiguration<BookmarkEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<BookmarkEntity> builder)
	{
		builder.ToTable("Bookmarks");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.CreatedOn).IsRequired();
		builder.Property(x => x.Id).IsRequired();
		builder.Property(x => x.IsDeleted).IsRequired();
		builder.Property(x => x.IsParent).IsRequired();
		builder.Property(x => x.ModifiedOn).IsRequired();
		builder.Property(x => x.Name).IsRequired();
		builder.Property(x => x.Order).IsRequired();
		builder.Property(x => x.SyncId).IsRequired();

		builder.HasIndex(x => x.SyncId)
			.HasIndexName("IX_Bookmarks_SyncId")
			.IsUnique();
		builder.HasIndex(x => x.ModifiedOn)
			.HasIndexName("IX_Bookmarks_ModifiedOn");

		builder.HasOne<BookmarkEntity>()
			.WithMany()
			.HasForeignKey(x => x.ParentId)
			.IsRequired(false);

		builder.HasOne<CustomerEntity>()
			.WithMany()
			.HasForeignKey(x => x.CustomerId)
			.IsRequired(false);
	}

	#endregion
}
