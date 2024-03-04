#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Data.Mappings;

[ExcludeFromCodeCoverage]
public class LogEventMap : EntityMappingConfiguration<LogEventEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<LogEventEntity> b)
	{
		b.ToTable("LogEvents", "dbo");
		b.HasKey(x => x.Id);

		b.Property(x => x.AcknowledgedOn).IsRequired(false);
		b.Property(x => x.CreatedOn).IsRequired();
		b.Property(x => x.Id).HasMaxLength(250).IsRequired();
		b.Property(x => x.IsDeleted).IsRequired();
		b.Property(x => x.Level).IsRequired();
		b.Property(x => x.LoggedOn).IsRequired();
		b.Property(x => x.Message).IsRequired(false);
		b.Property(x => x.ModifiedOn).IsRequired();
		b.Property(x => x.SyncId).IsRequired();
	}

	#endregion
}