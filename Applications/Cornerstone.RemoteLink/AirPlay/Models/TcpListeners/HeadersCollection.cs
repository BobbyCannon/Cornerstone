#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Models.TcpListeners;

public class HeadersCollection : IEnumerable<Header>
{
	#region Fields

	private readonly Dictionary<string, Header> _headers;

	#endregion

	#region Constructors

	public HeadersCollection()
	{
		_headers = new Dictionary<string, Header>(StringComparer.OrdinalIgnoreCase);
	}

	#endregion

	#region Properties

	public int Count => _headers.Count;

	public bool IsReadOnly => throw new NotImplementedException();

	public string this[string key]
	{
		get => string.Join(",", _headers[key].Values);
		set => _headers.Add(key, new Header(key, value));
	}

	public ICollection<string> Keys => _headers.Keys;

	public ICollection<string> Values => _headers.Values.Select(v => string.Join(",", v.Values)).ToList();

	#endregion

	#region Methods

	public void Add(string key, string value)
	{
		_headers.Add(key, new Header(key, value));
	}

	public void Add(string key, Header value)
	{
		_headers.Add(key, value);
	}

	public void Clear()
	{
		_headers.Clear();
	}

	public bool ContainsKey(string key)
	{
		return _headers.ContainsKey(key);
	}

	public IEnumerator<Header> GetEnumerator()
	{
		return _headers.Values.GetEnumerator();
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "T is annotated with DynamicallyAccessedMemberTypes.All.")]
	public T GetValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string key)
	{
		var value = this[key];

		var typeConverter = TypeDescriptor.GetConverter(typeof(T));
		return (T) typeConverter.ConvertFromString(value);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "T is annotated with DynamicallyAccessedMemberTypes.All.")]
	public IEnumerable<T> GetValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string key)
	{
		var typeConverter = TypeDescriptor.GetConverter(typeof(T));
		var values = _headers[key].Values;

		var vals = new List<T>();
		foreach (var value in values)
		{
			vals.Add((T) typeConverter.ConvertFromString(value));
		}

		return vals;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}