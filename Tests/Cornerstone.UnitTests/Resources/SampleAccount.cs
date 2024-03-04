#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.UnitTests.Resources;

public class SampleAccount : SyncEntity<int>, IClientEntity, IComparable<SampleAccount>, IComparable
{
	#region Properties

	public string EmailAddress { get; set; }

	public string FirstName { get; set; }

	public override int Id { get; set; }

	public DateTime LastClientUpdate { get; set; }

	public string LastName { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public int CompareTo(SampleAccount other)
	{
		if (ReferenceEquals(this, other))
		{
			return 0;
		}
		if (ReferenceEquals(null, other))
		{
			return 1;
		}
		var result = string.Compare(FirstName, other.FirstName, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(LastName, other.LastName, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = string.Compare(EmailAddress, other.EmailAddress, StringComparison.Ordinal);
		if (result != 0)
		{
			return result;
		}
		result = DateTime.Compare(LastClientUpdate, other.LastClientUpdate);
		if (result != 0)
		{
			return result;
		}

		return 0;
	}

	/// <inheritdoc />
	public int CompareTo(object obj)
	{
		return CompareTo(obj as SampleAccount);
	}

	/// <inheritdoc />
	public override HashSet<string> GetDefaultIncludedProperties(UpdateableAction action)
	{
		var properties = base.GetDefaultIncludedProperties(action)
			.AddRange(nameof(EmailAddress), nameof(FirstName), nameof(LastName));

		return properties;
	}

	#endregion
}