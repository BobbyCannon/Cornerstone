﻿#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Exceptions;

#endregion

namespace Cornerstone.Storage.Configuration;

/// <summary>
/// The configuration for an index property.
/// </summary>
public class IndexConfiguration
{
	#region Fields

	private string _combinedPropertyNames;
	private bool _isUnique;
	private readonly string _name;
	private readonly List<IPropertyConfiguration> _properties;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the index configuration.
	/// </summary>
	/// <param name="name"> The name of the index. </param>
	public IndexConfiguration(string name)
	{
		_name = name;
		_properties = [];
		_combinedPropertyNames = string.Join("", _properties.Select(x => x.MemberName));
	}

	#endregion

	#region Properties

	/// <summary>
	/// The index allows null. This would allow unique indexes to ignore null values.
	/// </summary>
	public bool AllowNull { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Add property to the index configuration.
	/// </summary>
	/// <param name="property"> The property configuration to be added. </param>
	public void AddProperty(IPropertyConfiguration property)
	{
		_properties.Add(property);
		_combinedPropertyNames = string.Join("", _properties.Select(x => x.MemberName));
	}

	/// <summary>
	/// Checks to see if this configuration is for the provided entity.
	/// </summary>
	/// <param name="entity"> The entity to test against. </param>
	/// <returns> True if this configuration is for the entity and false if otherwise. </returns>
	public bool IsMappingFor(object entity)
	{
		return _properties.Any(x => x.IsMappingFor(entity));
	}

	/// <summary>
	/// Marks the index as a unique.
	/// </summary>
	/// <returns> The configuration after updated. </returns>
	public IndexConfiguration IsUnique()
	{
		_isUnique = true;
		return this;
	}

	/// <summary>
	/// Validates the entity using this configuration.
	/// </summary>
	/// <param name="entity"> The entity to validate. </param>
	/// <param name="entityRepository"> The repository of entities. </param>
	public void Validate<T, T2>(object entity, IRepository<T, T2> entityRepository) where T : Entity<T2>
	{
		if (!(entity is T typedEntity))
		{
			throw new ArgumentNullException(nameof(typedEntity));
		}

		// Convert repository into local type so we can check new items
		var repository = (Repository<T, T2>) entityRepository;

		bool Predicate(T x)
		{
			if (_properties.Any(p => (p.IsNullable == true) && (p.GetValue(typedEntity) == null)))
			{
				return false;
			}

			var response = !ReferenceEquals(x, entity) && _properties.All(p => p.Matches(x, entity));
			return response;
		}

		var propertyValues = _properties.Select(x => x.GetValue(typedEntity)).ToList();

		if ((_combinedPropertyNames == "SyncId") && Equals(propertyValues[0], Guid.Empty))
		{
			return;
		}

		if (AllowNull && propertyValues.All(x => x == null))
		{
			return;
		}

		if (_isUnique && (repository.Any(Predicate) || repository.AnyNew(entity, Predicate)))
		{
			throw new ValidationException($"{_name}: Cannot insert duplicate row. The duplicate key value is ({string.Join(",", propertyValues)}).");
		}
	}

	#endregion
}