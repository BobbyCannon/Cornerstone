#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Arguments passed to the application at start up.
/// </summary>
public class ApplicationArguments
{
	#region Fields

	private readonly string _commandPrefix;
	private readonly Dictionary<string, string> _parsedArguments;

	#endregion

	#region Constructors

	public ApplicationArguments() : this("-")
	{

	}

	public ApplicationArguments(string commandPrefix)
	{
		_commandPrefix = string.IsNullOrEmpty(commandPrefix) ? "-" : commandPrefix;
		_parsedArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		CommandsProcessed = new SpeedyList<string>();
		OtherValues = new SpeedyList<string>();
		Raw = new SpeedyList<string>();
	}

	#endregion

	#region Properties

	public IReadOnlyList<string> Commands => _parsedArguments.Keys.ToList();

	public ISpeedyList<string> CommandsProcessed { get; }

	public int Count => _parsedArguments.Count;

	public string this[string key]
	{
		get => _parsedArguments.ContainsKey(key) ? _parsedArguments[key] : null;
		set
		{
			if (key == null)
			{
				return;
			}

			if (_parsedArguments.ContainsKey(key))
			{
				_parsedArguments[key] = value;
				return;
			}

			_parsedArguments.Add(key, value);
		}
	}

	public SpeedyList<string> OtherValues { get; }

	public SpeedyList<string> Raw { get; }

	#endregion

	#region Methods

	public void Add(string key, string value)
	{
		_parsedArguments.Add(CleanupKey(key), value);
	}

	public void AddIfMissing(string key, string value)
	{
		if (HasArgument(key))
		{
			return;
		}

		Add(key, value);
	}

	public T ConvertTo<T>(string key)
	{
		var value = this[key];
		var type = typeof(T);

		#if NETSTANDARD2_0
		if (type.IsEnum)
		{
			try
			{
				return (T) Enum.Parse(type, value);
			}
			catch
			{
				return default;
			}
		}
		#else
		if (type.IsEnum && Enum.TryParse(type, value, true, out var eValue))
		{
			return (T) eValue;
		}
		#endif

		if ((type == typeof(bool)) && bool.TryParse(value, out var bValue))
		{
			return (T) (object) bValue;
		}

		if ((type == typeof(int)) && int.TryParse(value, out var iValue))
		{
			return (T) (object) iValue;
		}

		if ((type == typeof(uint)) && uint.TryParse(value, out var uiValue))
		{
			return (T) (object) uiValue;
		}

		return default;
	}

	public bool HasArgument(string key)
	{
		return _parsedArguments.ContainsKey(key);
	}

	public static ApplicationArguments Parse(string commandPrefix = "-", params string[] arguments)
	{
		var response = new ApplicationArguments(commandPrefix);
		response.Parse(arguments);
		return response;
	}

	public void Parse(IList<string> arguments)
	{
		Raw.Load(arguments);

		for (var i = 0; i < arguments.Count; i++)
		{
			if (IsKey(arguments[i]))
			{
				var key = CleanupKey(arguments[i]);
				string val = null;

				if (((i + 1) < arguments.Count) && !IsKey(arguments[i + 1]))
				{
					val = arguments[i + 1];
					i++;
				}

				_parsedArguments[key] = val;
			}
			else if (arguments[i] != null)
			{
				OtherValues.Add(arguments[i]);
			}
		}
	}

	public string Read(string key, string defaultValue)
	{
		return HasArgument(key) ? this[key] : defaultValue;
	}

	public void Remove(string name)
	{
		_parsedArguments.Remove(name);
	}

	public override string ToString()
	{
		return string.Join(" ", Raw);
	}

	private string CleanupKey(string key)
	{
		return IsKey(key) ? key.Substring(_commandPrefix.Length) : key;
	}

	private bool IsKey(string value)
	{
		return value?.StartsWith(_commandPrefix) ?? false;
	}

	#endregion
}