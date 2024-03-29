#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Data.Mappings;

[ExcludeFromCodeCoverage]
public class GroupMap : EntityMappingConfiguration<GroupEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<GroupEntity> b)
	{
		b.ToTable("Groups", "dbo");
		b.HasKey(x => x.Id);

		b.Property(x => x.CreatedOn).HasColumnName("CreatedOn").IsRequired();
		b.Property(x => x.Description).HasColumnName("Description").HasMaxLength(4000).IsRequired();
		b.Property(x => x.Id).HasColumnName("Id").IsRequired();
		b.Property(x => x.ModifiedOn).HasColumnName("ModifiedOn").IsRequired();
		b.Property(x => x.Name).HasColumnName("Name").HasMaxLength(256).IsRequired();
	}

	#endregion
}