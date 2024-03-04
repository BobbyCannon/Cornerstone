#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Data.Mappings;

[ExcludeFromCodeCoverage]
public class FoodMap : EntityMappingConfiguration<FoodEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<FoodEntity> b)
	{
		b.ToTable("Foods", "dbo");
		b.HasKey(x => x.Id);

		b.Property(x => x.CreatedOn).HasColumnName("CreatedOn").IsRequired();
		b.Property(x => x.Id).HasColumnName("Id").IsRequired();
		b.Property(x => x.ModifiedOn).HasColumnName("ModifiedOn").IsRequired();
		b.Property(x => x.Name).HasColumnName("Name").HasMaxLength(256).IsRequired();

		#if NET6_0_OR_GREATER
		b.HasIndex(x => x.Name).HasDatabaseName("IX_Foods_Name").IsUnique();
		#else
		b.HasIndex(x => x.Name).HasName("IX_Foods_Name").IsUnique();
		#endif
	}

	#endregion
}