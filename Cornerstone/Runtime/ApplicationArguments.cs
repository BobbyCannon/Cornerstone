#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Runtime;

public class ApplicationArguments
{
	#region Fields

	private string[] _args;
	private readonly List<string> _others;
	private readonly Dictionary<string, string> _parsed;
	private string _prefix;

	#endregion

	#region Constructors

	public ApplicationArguments()
	{
		_prefix = "-";
		_parsed = new(8, StringComparer.Ordinal);
		_others = new(4);
	}

	#endregion

	#region Properties

	public string this[string key] => _parsed.TryGetValue(key, out var v) ? v : null;

	public IEnumerable<string> OtherValues => _others;

	#endregion

	#region Methods

	public T GetValue<T>(string key)
	{
		return TryGetValue<T>(key, out var value) ? value : default;
	}

	public void Parse(string[] args, string prefix = null)
	{
		if (args == null)
		{
			return;
		}

		_args = args;
		_prefix = prefix ?? "-";
		_parsed.Clear();
		_others.Clear();

		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i];
			if (string.IsNullOrEmpty(arg))
			{
				continue;
			}

			var argSpan = arg.AsSpan();

			if (argSpan.StartsWith(_prefix))
			{
				var keySpan = argSpan[_prefix.Length..];
				string value = null;

				if ((i + 1) < args.Length)
				{
					var nextArg = args[i + 1];
					if (!string.IsNullOrEmpty(nextArg))
					{
						var nextSpan = nextArg.AsSpan();
						if (!nextSpan.StartsWith(_prefix))
						{
							value = nextArg;
							i++;
						}
					}
				}

				_parsed[keySpan.ToString()] = value;
			}
			else
			{
				_others.Add(arg);
			}
		}
	}

	public void ParseFromBrowser(string[] args)
	{
		if (args == null)
		{
			return;
		}

		foreach (var arg in args)
		{
			if (!Uri.TryCreate(arg, UriKind.RelativeOrAbsolute, out var uri))
			{
				continue;
			}

			Parse(uri.ToApplicationArguments());
			break;
		}
	}

	public override string ToString()
	{
		return string.Join(" ", _args);
	}

	public bool TryGetValue<T>(string key, out T value)
	{
		value = default!;
		if (!_parsed.TryGetValue(key, out var str) || str is null)
		{
			return false;
		}

		if (typeof(T) == typeof(string))
		{
			value = (T) (object) str;
			return true;
		}
		if ((typeof(T) == typeof(bool)) && bool.TryParse(str, out var b))
		{
			value = (T) (object) b;
			return true;
		}
		if ((typeof(T) == typeof(int)) && int.TryParse(str, out var i))
		{
			value = (T) (object) i;
			return true;
		}
		if ((typeof(T) == typeof(uint)) && uint.TryParse(str, out var u))
		{
			value = (T) (object) u;
			return true;
		}
		if ((typeof(T) == typeof(double)) && double.TryParse(str, out var d))
		{
			value = (T) (object) d;
			return true;
		}
		if ((typeof(T) == typeof(float)) && float.TryParse(str, out var f))
		{
			value = (T) (object) f;
			return true;
		}
		if ((typeof(T) == typeof(Guid)) && Guid.TryParse(str, out var g))
		{
			value = (T) (object) g;
			return true;
		}

		if (typeof(T).IsEnum
			&& Enum.TryParse(typeof(T), str, true, out var eValue))
		{
			value = (T) eValue;
			return true;
		}

		// Add more types as needed (DateTimeOffset, double, Guid, etc.)

		return false;
	}

	#endregion
}