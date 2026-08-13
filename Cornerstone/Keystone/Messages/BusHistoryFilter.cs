#region References

using System;
using System.Collections.Generic;
using System.Globalization;

#endregion

namespace Cornerstone.Keystone.Messages;

/// <summary>
/// Text-driven filter for bus history rows. Empty text matches everything.
/// Tokens are whitespace-separated and combined with AND.
/// Structured keys: channel: (ChannelName contains, case-insensitive),
/// type: (exact int, comma-separated OR), error:true|false.
/// Remaining tokens are free-text substrings over Name, ErrorMessage, and ChannelName (view-oriented).
/// </summary>
public sealed class BusHistoryFilter
{
	#region Fields

	private static readonly BusHistoryFilter MatchAllInstance = new(
		string.Empty,
		Array.Empty<string>(),
		Array.Empty<int>(),
		Array.Empty<string>(),
		ErrorConstraint.Any);

	private readonly string[] _channelContains;
	private readonly int[] _types;
	private readonly string[] _freeText;
	private readonly ErrorConstraint _errorConstraint;

	#endregion

	#region Constructors

	private BusHistoryFilter(
		string sourceText,
		string[] channelContains,
		int[] types,
		string[] freeText,
		ErrorConstraint errorConstraint)
	{
		SourceText = sourceText ?? string.Empty;
		_channelContains = channelContains ?? Array.Empty<string>();
		_types = types ?? Array.Empty<int>();
		_freeText = freeText ?? Array.Empty<string>();
		_errorConstraint = errorConstraint;
		IsMatchAll = (SourceText.Length == 0)
			&& (_channelContains.Length == 0)
			&& (_types.Length == 0)
			&& (_freeText.Length == 0)
			&& (_errorConstraint == ErrorConstraint.Any);
	}

	#endregion

	#region Properties

	/// <summary>
	/// True when no constraints are active (empty or whitespace-only source).
	/// </summary>
	public bool IsMatchAll { get; }

	/// <summary>
	/// Text that produced this filter (trimmed).
	/// </summary>
	public string SourceText { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Parse filter text. Null or whitespace yields a match-all filter.
	/// Invalid type integers are ignored. Unknown key:value tokens become free text.
	/// </summary>
	public static BusHistoryFilter Parse(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return MatchAllInstance;
		}

		var trimmed = text.Trim();
		var channels = new List<string>();
		var types = new List<int>();
		var freeText = new List<string>();
		var errorConstraint = ErrorConstraint.Any;

		var parts = trimmed.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < parts.Length; i++)
		{
			var part = parts[i];
			var colon = part.IndexOf(':');
			if ((colon > 0) && (colon < part.Length - 1))
			{
				var key = part.Substring(0, colon);
				var value = part.Substring(colon + 1);
				if (key.Equals("channel", StringComparison.OrdinalIgnoreCase))
				{
					if (value.Length > 0)
					{
						channels.Add(value);
					}

					continue;
				}

				if (key.Equals("type", StringComparison.OrdinalIgnoreCase))
				{
					var typeParts = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
					for (var t = 0; t < typeParts.Length; t++)
					{
						if (int.TryParse(typeParts[t].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var typeId))
						{
							types.Add(typeId);
						}
					}

					continue;
				}

				if (key.Equals("error", StringComparison.OrdinalIgnoreCase))
				{
					if (value.Equals("true", StringComparison.OrdinalIgnoreCase)
						|| value.Equals("1", StringComparison.OrdinalIgnoreCase)
						|| value.Equals("yes", StringComparison.OrdinalIgnoreCase))
					{
						errorConstraint = ErrorConstraint.ErrorsOnly;
					}
					else if (value.Equals("false", StringComparison.OrdinalIgnoreCase)
						|| value.Equals("0", StringComparison.OrdinalIgnoreCase)
						|| value.Equals("no", StringComparison.OrdinalIgnoreCase))
					{
						errorConstraint = ErrorConstraint.NoErrors;
					}

					continue;
				}
			}

			freeText.Add(part);
		}

		if ((channels.Count == 0) && (types.Count == 0) && (freeText.Count == 0) && (errorConstraint == ErrorConstraint.Any))
		{
			return MatchAllInstance;
		}

		return new BusHistoryFilter(
			trimmed,
			channels.ToArray(),
			types.ToArray(),
			freeText.ToArray(),
			errorConstraint);
	}

	/// <summary>
	/// Evaluate against a completed publish result (record path). Free-text tokens
	/// match message type name when present.
	/// </summary>
	public bool Matches(ChannelMessagePublishResult result)
	{
		if (IsMatchAll)
		{
			return true;
		}

		var name = result.Message?.GetType().Name;
		if (string.IsNullOrEmpty(name))
		{
			name = result.Type.ToString();
		}

		return MatchesCore(result.ChannelName, result.Type, result.HadError, name, result.ErrorMessage);
	}

	/// <summary>
	/// Evaluate against a stored history row (view path).
	/// </summary>
	public bool Matches(ChannelMessageHistory entry)
	{
		if (entry is null)
		{
			return false;
		}

		if (IsMatchAll)
		{
			return true;
		}

		return MatchesCore(entry.ChannelName, entry.Type, entry.HadError, entry.Name, entry.ErrorMessage);
	}

	private bool MatchesCore(string channelName, int type, bool hadError, string name, string errorMessage)
	{
		if (_errorConstraint == ErrorConstraint.ErrorsOnly && !hadError)
		{
			return false;
		}

		if (_errorConstraint == ErrorConstraint.NoErrors && hadError)
		{
			return false;
		}

		if (_types.Length > 0)
		{
			var typeOk = false;
			for (var i = 0; i < _types.Length; i++)
			{
				if (_types[i] == type)
				{
					typeOk = true;
					break;
				}
			}

			if (!typeOk)
			{
				return false;
			}
		}

		var channel = channelName ?? string.Empty;
		for (var i = 0; i < _channelContains.Length; i++)
		{
			if (channel.IndexOf(_channelContains[i], StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
		}

		if (_freeText.Length > 0)
		{
			var nameText = name ?? string.Empty;
			var errorText = errorMessage ?? string.Empty;
			for (var i = 0; i < _freeText.Length; i++)
			{
				var token = _freeText[i];
				if ((nameText.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
					&& (errorText.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
					&& (channel.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0))
				{
					return false;
				}
			}
		}

		return true;
	}

	#endregion

	#region Enumerations

	private enum ErrorConstraint
	{
		Any = 0,
		ErrorsOnly = 1,
		NoErrors = 2
	}

	#endregion
}
