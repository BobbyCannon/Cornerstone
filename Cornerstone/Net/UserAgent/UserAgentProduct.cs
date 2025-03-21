#region References

using System;
using System.Text.RegularExpressions;
using Cornerstone.Runtime;

#if NETSTANDARD
using Cornerstone.Extensions;
#endif

#endregion

namespace Cornerstone.Net.UserAgent;

/// <summary>
/// Represents a product for a user agent.
/// </summary>
public class UserAgentProduct
{
	#region Fields

	private static readonly Regex _androidRegex;
	private string _originalString;
	private static readonly Regex _pixelRegex;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize a user agent product.
	/// </summary>
	protected UserAgentProduct()
	{
	}

	static UserAgentProduct()
	{
		_androidRegex = new Regex("Android[ ]?(?<v>[\\w]+)?", RegexOptions.Compiled);
		_pixelRegex = new Regex("Pixel[ ]?(?<v>[\\w]+)?", RegexOptions.Compiled);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The bitness (x86, x64, etc)
	/// </summary>
	public string Bitness { get; set; }

	/// <summary>
	/// The comment.
	/// </summary>
	public string Comment { get; set; }

	/// <summary>
	/// The name for the device.
	/// </summary>
	public string Device { get; set; }

	/// <summary>
	/// The type of the device.
	/// </summary>
	public DeviceType DeviceType { get; set; }

	/// <summary>
	/// The product name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The operating system.
	/// </summary>
	public DevicePlatform OperatingSystem { get; set; }

	/// <summary>
	/// The version of the OS.
	/// </summary>
	public string OperatingSystemVersion { get; set; }

	/// <summary>
	/// The version of the product.
	/// </summary>
	public string Version { get; set; }

	#endregion

	#region Methods

	private void ProcessComment()
	{
		if (string.IsNullOrWhiteSpace(Comment))
		{
			return;
		}

		var match = _androidRegex.Match(Comment);
		if (match.Success)
		{
			OperatingSystem = DevicePlatform.Android;
			#if !NETSTANDARD
			OperatingSystemVersion = match.Groups.TryGetValue("v", out var v) ? v.Value : null;
			#else
			OperatingSystemVersion = match.Groups["v"].Value;
			#endif
			(Device, DeviceType) = ParseDevice(Comment);
			Bitness = ParseBitness(Comment);
			return;
		}

		if (Comment.Contains("Windows NT 10.0"))
		{
			OperatingSystem = DevicePlatform.Windows;
			OperatingSystemVersion = "10.0";
			(Device, DeviceType) = ParseDevice(Comment, null, DeviceType.Desktop);
			Bitness = ParseBitness(Comment);
		}
		else if (Comment.Contains("Windows NT 6.3"))
		{
			OperatingSystem = DevicePlatform.Windows;
			OperatingSystemVersion = "8.1";
			(Device, DeviceType) = ParseDevice(Comment, null, DeviceType.Desktop);
			Bitness = ParseBitness(Comment);
		}
		else if (Comment.Contains("Linux"))
		{
			OperatingSystem = DevicePlatform.Linux;
			OperatingSystemVersion = null;
			(Device, DeviceType) = ParseDevice(Comment, null, DeviceType.Desktop);
			Bitness = ParseBitness(Comment);
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return _originalString;
	}

	/// <summary>
	/// Process the match in the agent string.
	/// </summary>
	/// <param name="match"> The match. </param>
	/// <returns> The product for the user agent. </returns>
	protected static UserAgentProduct Process(Match match)
	{
		var p = new UserAgentProduct { _originalString = match.Value };

		foreach (var item in match.Groups)
		{
			switch (item)
			{
				case Match:
				{
					continue;
				}
				case Group group:
				{
					#if NETSTANDARD2_0
					var name = group.GetMemberValue("Name");
					#else
					var name = group.Name;
					#endif
					switch (name)
					{
						case "p":
						{
							var parts = group.Value.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
							p.Name = parts.Length >= 1 ? parts[0] : string.Empty;
							p.Version = parts.Length >= 2 ? parts[1] : string.Empty;
							continue;
						}
						case "c":
						{
							p.Comment = group.Value;
							continue;
						}
						case "s":
						{
							continue;
						}
					}
					break;
				}
			}
		}

		p.ProcessComment();
		return p;
	}

	private string ParseBitness(string comment)
	{
		if (comment.Contains("x86_64"))
		{
			return "x86_64";
		}

		if (comment.Contains("x64"))
		{
			return "x64";
		}

		return null;
	}

	private (string, DeviceType) ParseDevice(string comment, string deviceDefault = null, DeviceType defaultDeviceType = DeviceType.Unknown)
	{
		var match = _pixelRegex.Match(Comment);
		if (match.Success)
		{
			return (match.ToString(), DeviceType.Phone);
		}

		if (comment.Contains("KFRAWI"))
		{
			return ("Amazon Fire HD 8 (2022)", DeviceType.Tablet);
		}

		return (deviceDefault, defaultDeviceType);
	}

	#endregion
}