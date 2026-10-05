#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.RemoteLink.AirPlay.Utils;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Models.TcpListeners;

public class Header
{
	#region Fields

	private readonly string _hex;

	private List<string> _values;

	#endregion

	#region Constructors

	public Header(string hex)
	{
		_hex = hex ?? throw new ArgumentNullException(nameof(hex));

		Initialize();
	}

	public Header(string name, string value)
	{
		Name = name;
		_values = value.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).ToList();
	}

	public Header(string name, params string[] values)
	{
		Name = name;
		_values = values.ToList();
	}

	#endregion

	#region Properties

	public bool IsValid { get; private set; } = true;

	public string Name { get; private set; }

	public IEnumerable<string> Values => _values;

	#endregion

	#region Methods

	private void Initialize()
	{
		// Split hex by ':' (3A)
		var data = _hex.Split("3A", StringSplitOptions.RemoveEmptyEntries).Select(h => h.Trim()).ToArray();
		if (data?.Any() == false)
		{
			IsValid = false;
			return;
		}

		Name = ResolveName(data[0]);
		var hValue = data[1];

		_values = ResolveValues(hValue);
	}

	private string ResolveName(string hex)
	{
		var bytes = hex.HexToBytes();
		return Encoding.ASCII.GetString(bytes);
	}

	private List<string> ResolveValues(string hex)
	{
		// Split hex by ',' (2C)
		var vals = hex.Split("2C", StringSplitOptions.RemoveEmptyEntries);

		var _values = new List<string>();

		foreach (var val in vals)
		{
			var bytes = val.HexToBytes();
			var hVal = Encoding.ASCII.GetString(bytes).Trim();
			_values.Add(hVal);
		}

		return _values;
	}

	#endregion
}