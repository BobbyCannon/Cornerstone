#region References

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Net.UserAgent;

/// <summary>
/// Represents the data contained in a user agent string.
/// https://www.rfc-editor.org/rfc/rfc7231#section-5.5.3
/// https://deviceatlas.com/blog/list-of-user-agent-strings
/// </summary>
/// <example>
/// Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36
/// Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36 OPR/38.0.2220.41
/// Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59
/// Mozilla/5.0 (iPhone; CPU iPhone OS 13_5_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1.1 Mobile/15E148 Safari/604.1
/// Mozilla/5.0 (compatible; MSIE 9.0; Windows Phone OS 7.5; Trident/5.0; IEMobile/9.0)
/// Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)
/// curl/7.64.1
/// PostmanRuntime/7.26.5
/// </example>
public class UserAgent : UserAgentProduct
{
	#region Constants

	private const string _pattern = "(?<p>[^\\s]+)(?<s>[\\s])?(?<c>[(]+[^)]+[\\s]?[)])?";

	#endregion

	#region Fields

	private static readonly Regex _compilePattern;
	private string _originalString;

	#endregion

	#region Constructors

	static UserAgent()
	{
		_compilePattern = new Regex(_pattern, RegexOptions.Compiled);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Parse a string into a user agent.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <returns> The user agent the string represents. </returns>
	public static UserAgent Parse(string value)
	{
		var agent = new UserAgent
		{
			_originalString = value
		};

		var products = agent.ParseProducts(value).ToArray();
		var lastProduct = products.FirstOrDefault();

		if (lastProduct == null)
		{
			return null;
		}

		agent.Name = lastProduct.Name;
		agent.Version = lastProduct.Version;

		agent.Bitness = products.FirstOrDefault(x => x.Bitness != null)?.Bitness;
		agent.Device = products.FirstOrDefault(x => x.Device != null)?.Device;
		agent.DeviceType = products.FirstOrDefault(x => x.DeviceType != DeviceType.Unknown)?.DeviceType ?? DeviceType.Unknown;
		agent.OperatingSystem = products.FirstOrDefault(x => x.OperatingSystem != DevicePlatform.Unknown)?.OperatingSystem ?? DevicePlatform.Unknown;
		agent.OperatingSystemVersion = products.FirstOrDefault(x => x.OperatingSystemVersion != null)?.OperatingSystemVersion;

		Cleanup(agent);

		return agent;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (_originalString != null)
		{
			return _originalString;
		}

		var builder = new StringBuilder();
		builder.Append($"{Name}/{Version}");
		return builder.ToString();
	}

	/// <summary>
	/// To a small summary string.
	/// </summary>
	/// <returns> The agent in a summary string. </returns>
	public string ToSummary()
	{
		var response = new StringBuilder($"{Name} {Version}");

		if (Device != null)
		{
			response.Append(" ");
			response.Append(Device);
		}

		if (OperatingSystem != DevicePlatform.Unknown)
		{
			response.Append(" ");
			response.Append(OperatingSystem);
		}

		if (OperatingSystemVersion != null)
		{
			response.Append(" ");
			response.Append(OperatingSystemVersion);
		}

		if (Bitness != null)
		{
			response.Append(" ");
			response.Append(Bitness);
		}

		return response.ToString();
	}

	private static void Cleanup(UserAgent agent)
	{
		if (agent.Name is "Edg" or "EdgA")
		{
			agent.Name = "Edge";
		}
	}

	private IEnumerable<UserAgentProduct> ParseProducts(string value)
	{
		var matches = _compilePattern.Matches(value);
		var response = new List<UserAgentProduct>(matches.Count);

		foreach (Match match in matches)
		{
			var p = Process(match);
			response.Insert(0, p);
		}

		return response.ToArray();
	}

	#endregion
}