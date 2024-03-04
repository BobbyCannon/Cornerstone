#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Data.Mappings;

[ExcludeFromCodeCoverage]
public class SettingMap : EntityMappingConfiguration<SettingEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<SettingEntity> b)
	{
		b.ToTable("Settings", "dbo");
		b.HasKey(x => x.Id);

		b.Property(x => x.CreatedOn).HasColumnName("CreatedOn").IsRequired();
		b.Property(x => x.Id).HasColumnName("Id").IsRequired();
		b.Property(x => x.IsDeleted).HasColumnName("IsDeleted").IsRequired();
		b.Property(x => x.ModifiedOn).HasColumnName("ModifiedOn").IsRequired();
		b.Property(x => x.ServerName).HasColumnName("Name").HasMaxLength(256).IsRequired();
		b.Property(x => x.ServerValue).HasColumnName("Value").IsRequired();
		b.Property(x => x.SyncId).HasColumnName("SyncId").IsRequired();

		b.HasIndex(x => x.ServerName).HasIndexName("IX_Settings_Name").IsUnique();
		b.HasIndex(x => x.SyncId).HasIndexName("IX_Settings_SyncId").IsUnique();
	}

	#endregion
}