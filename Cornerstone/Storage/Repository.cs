#region References

using System;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type contained in the repository. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
[Serializable]
internal class Repository<T, T2>
	: Repository<T>,
		IRepository<T, T2>
	where T : Entity<T2>
{
}

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type contained in the repository. </typeparam>
internal abstract class Repository<T>
	: IRepository<T>
	where T : Entity
{
}

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type of the entity of the collection. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
public interface IRepository<T, in T2>
	: IRepository<T>
	where T : Entity<T2>
{
}

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type of the entity of the collection. </typeparam>
public interface IRepository<T>
	where T : Entity
{
}