#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Plist;

/// <summary>
/// Represents a cached type used during serialization by a <see cref="DataContractBinaryPlistSerializer" />.
/// </summary>
internal sealed class TypeCacheItem
{
	#region Fields

	private readonly bool hasCustomContract;

	[DynamicallyAccessedMembers(
		DynamicallyAccessedMemberTypes.PublicFields
		| DynamicallyAccessedMemberTypes.NonPublicFields
		| DynamicallyAccessedMemberTypes.PublicProperties
		| DynamicallyAccessedMemberTypes.NonPublicProperties)]
	private readonly Type type;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the TypeCacheItem class.
	/// </summary>
	/// <param name="type"> The type to cache. </param>
	public TypeCacheItem(
		[DynamicallyAccessedMembers(
			DynamicallyAccessedMemberTypes.PublicFields
			| DynamicallyAccessedMemberTypes.NonPublicFields
			| DynamicallyAccessedMemberTypes.PublicProperties
			| DynamicallyAccessedMemberTypes.NonPublicProperties)]
		Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type", "type cannot be null.");
		}

		this.type = type;
		hasCustomContract = type.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0;
		InitializeFields();
		InitializeProperties();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the collection of concrete or simulated <see cref="DataMemberAttribute" />s for the type's fields.
	/// </summary>
	public IList<DataMemberAttribute> FieldMembers { get; private set; }

	/// <summary>
	/// Gets a collection of the type's fields.
	/// </summary>
	public IList<FieldInfo> Fields { get; private set; }

	/// <summary>
	/// Gets a collection of the type's properties.
	/// </summary>
	public IList<PropertyInfo> Properties { get; private set; }

	/// <summary>
	/// Gets a collection of concrete or simulated <see cref="DataMemberAttribute" />s for the type's properties.
	/// </summary>
	public IList<DataMemberAttribute> PropertyMembers { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Initializes this instance's field-related properties.
	/// </summary>
	private void InitializeFields()
	{
		FieldMembers = new List<DataMemberAttribute>();
		Fields = new List<FieldInfo>();

		var fields = hasCustomContract ? type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : type.GetFields(BindingFlags.Instance | BindingFlags.Public);

		var tuples = from f in fields
			let attr = f.GetCustomAttributes(false)
			let member = attr.OfType<DataMemberAttribute>().FirstOrDefault()
			where !f.IsLiteral && (attr.OfType<IgnoreDataMemberAttribute>().Count() == 0)
			select new
			{
				Info = f,
				Member = member
			};

		foreach (var tuple in tuples.Where(t => !hasCustomContract || (t.Member != null)))
		{
			var member = tuple.Member != null
				? tuple.Member
				: new DataMemberAttribute
				{
					EmitDefaultValue = true,
					IsRequired = false
				};

			member.Name = !string.IsNullOrEmpty(member.Name) ? member.Name : tuple.Info.Name;

			FieldMembers.Add(member);
			Fields.Add(tuple.Info);
		}
	}

	/// <summary>
	/// Initializes this instance's property-related properties.
	/// </summary>
	private void InitializeProperties()
	{
		Properties = new List<PropertyInfo>();
		PropertyMembers = new List<DataMemberAttribute>();

		var properties = hasCustomContract ? type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

		var tuples = from p in properties
			let attr = p.GetCustomAttributes(false)
			let member = attr.OfType<DataMemberAttribute>().FirstOrDefault()
			where p.CanRead && p.CanWrite && (attr.OfType<IgnoreDataMemberAttribute>().Count() == 0)
			select new
			{
				Info = p,
				Member = member
			};

		foreach (var tuple in tuples.Where(t => !hasCustomContract || (t.Member != null)))
		{
			var member = tuple.Member != null
				? tuple.Member
				: new DataMemberAttribute
				{
					EmitDefaultValue = true,
					IsRequired = false
				};

			member.Name = !string.IsNullOrEmpty(member.Name) ? member.Name : tuple.Info.Name;

			PropertyMembers.Add(member);
			Properties.Add(tuple.Info);
		}
	}

	#endregion
}