#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Data.Mappings;

[ExcludeFromCodeCoverage]
public class PetTypeMap : EntityMappingConfiguration<PetTypeEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<PetTypeEntity> b)
	{
		b.ToTable("PetType", "dbo");
		b.HasKey(x => x.Id);

		b.Property(x => x.CreatedOn).HasColumnName("CreatedOn").IsRequired();
		b.Property(x => x.Id).HasColumnName("PetTypeId").HasMaxLength(25).IsRequired();
		b.Property(x => x.ModifiedOn).HasColumnName("ModifiedOn").IsRequired();
		b.Property(x => x.Type).HasColumnName("Type").HasMaxLength(200).IsRequired(false);
	}

	#endregion
}