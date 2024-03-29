#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Data.Mappings;

[ExcludeFromCodeCoverage]
public class GroupMemberMap : EntityMappingConfiguration<GroupMemberEntity>
{
	#region Methods

	public override void Map(EntityTypeBuilder<GroupMemberEntity> b)
	{
		b.ToTable("GroupMembers", "dbo");
		b.HasKey(x => x.Id);

		b.Property(x => x.CreatedOn).HasColumnName("CreatedOn").IsRequired();
		b.Property(x => x.GroupId).HasColumnName("GroupId").IsRequired();
		b.Property(x => x.Id).HasColumnName("Id").IsRequired();
		b.Property(x => x.MemberId).HasColumnName("MemberId").IsRequired();
		b.Property(x => x.MemberSyncId).HasColumnName("MemberSyncId").IsRequired();
		b.Property(x => x.ModifiedOn).HasColumnName("ModifiedOn").IsRequired();
		b.Property(x => x.Role).HasColumnName("Role").HasMaxLength(4000).IsRequired();

		#if NET6_0_OR_GREATER
		b.HasIndex(x => x.GroupId).HasDatabaseName("IX_GroupMembers_GroupId");
		b.HasIndex(x => x.MemberId).HasDatabaseName("IX_GroupMembers_MemberId");
		b.HasIndex(x => x.MemberSyncId).HasDatabaseName("IX_GroupMembers_MemberSyncId");
		#else
		b.HasIndex(x => x.GroupId).HasName("IX_GroupMembers_GroupId");
		b.HasIndex(x => x.MemberId).HasName("IX_GroupMembers_MemberId");
		b.HasIndex(x => x.MemberSyncId).HasName("IX_GroupMembers_MemberSyncId");
		#endif

		b.HasOne(x => x.Group).WithMany(x => x.Members).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
		b.HasOne(x => x.Member).WithMany(x => x.Groups).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
	}

	#endregion
}