#region References

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#endregion

namespace Cornerstone.EntityFramework;

public abstract class EntityMappingConfiguration<T> : IEntityMappingConfiguration<T> where T : class
{
	#region Methods

	public abstract void Map(EntityTypeBuilder<T> builder);

	public object Map(ModelBuilder builder)
	{
		var entity = builder.Entity<T>();
		Map(entity);
		return entity;
	}

	#endregion
}