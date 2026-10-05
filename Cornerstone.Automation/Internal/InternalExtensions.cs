#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cornerstone.Automation.Desktop;
using Cornerstone.Automation.Desktop.Elements;
using Cornerstone.Extensions;
using Cornerstone.Platforms.Windows;
using Interop.UIAutomationClient;
using ExpandCollapseState = Cornerstone.Automation.Desktop.Pattern.ExpandCollapseState;
using ToggleState = Cornerstone.Automation.Desktop.Pattern.ToggleState;

#endregion

namespace Cornerstone.Automation.Internal;

internal static class InternalExtensions
{
	#region Methods

	public static JsonArray AsJsonArray(this JsonNode node)
	{
		return node as JsonArray;
	}

	/// <summary>
	/// Deserialize JSON data into a JsonNode.
	/// </summary>
	/// <param name="data"> The JSON data to deserialize. </param>
	/// <returns> The JsonNode of the data. </returns>
	public static JsonNode AsJsonNode(this string data)
	{
		if (string.IsNullOrWhiteSpace(data))
		{
			return null;
		}

		try
		{
			return JsonNode.Parse(data);
		}
		catch (JsonException)
		{
			return JsonValue.Create(data);
		}
	}

	public static JsonObject AsJsonObject(this JsonNode node)
	{
		return node as JsonObject;
	}

	public static string AsString(this JsonNode node)
	{
		if (node == null)
		{
			return null;
		}

		if (node is JsonValue value)
		{
			if (value.TryGetValue(out string text))
			{
				return text;
			}

			if (value.TryGetValue(out JsonElement element))
			{
				return element.ValueKind == JsonValueKind.String
					? element.GetString()
					: element.ToString();
			}

			return value.ToString();
		}

		return node.ToJsonString();
	}

	/// <summary>
	/// Return the first string that is not null or empty.
	/// </summary>
	/// <param name="collection"> The collection of string to parse. </param>
	public static string FirstValue(this IEnumerable<string> collection)
	{
		return collection.FirstOrDefault(item => !string.IsNullOrEmpty(item));
	}

	/// <summary>
	/// Formats the string to be able to include inside inner string.
	/// </summary>
	/// <param name="source"> The source string value. </param>
	/// <returns> The string formatted to be place inside inner string. </returns>
	public static string FormatForInnerString(this string source)
	{
		return source.Replace("\\", "\\\\");
	}

	public static bool GetBoolean(this JsonNode node, string name)
	{
		var child = node?[name];
		if (child is JsonValue value && value.TryGetValue(out bool flag))
		{
			return flag;
		}

		return bool.TryParse(child.AsString(), out var parsed) && parsed;
	}

	public static int GetInt(this JsonNode node, string name)
	{
		var child = node?[name];
		if (child is JsonValue value)
		{
			if (value.TryGetValue(out int number))
			{
				return number;
			}

			if (value.TryGetValue(out double floating))
			{
				return (int) floating;
			}
		}

		return int.TryParse(child.AsString(), out var parsed) ? parsed : 0;
	}

	public static string GetString(this JsonNode node, string name)
	{
		return node?[name].AsString();
	}

	public static IntPtr Refresh(SafeProcess process, TimeSpan? timeout = null)
	{
		var handle = IntPtr.Zero;

		if (process == null)
		{
			return handle;
		}

		Utility.WaitUntil(() =>
			{
				var frameHosts = Process.GetProcessesByName("ApplicationFrameHost");

				foreach (var host in frameHosts)
				{
					using var application = new Application(host);
					application.Refresh();

					foreach (var c in application.Children)
					{
						if (!(c is Window window))
						{
							continue;
						}

						if (window.NativeElement.CurrentProcessId == process.Id)
						{
							window.Dispose();
							handle = window.Handle;
							return true;
						}

						foreach (var cc in c.Children)
						{
							if (!(cc is Window ww))
							{
								continue;
							}

							if (ww.NativeElement.CurrentProcessId != process.Id)
							{
								continue;
							}

							ww.Dispose();
							handle = window.Handle;
							return true;
						}
					}
				}

				return false;
			},
			(int) (timeout?.TotalMilliseconds ?? 0),
			25
		);

		return handle;
	}

	/// <summary>
	/// Converts the string to an integer.
	/// </summary>
	/// <param name="item"> The item to convert to an integer. </param>
	/// <returns> The JSON data of the object. </returns>
	public static int ToInt(this string item)
	{
		return int.TryParse(item, out var response) ? response : 0;
	}

	internal static ToggleState Convert(this Interop.UIAutomationClient.ToggleState state)
	{
		return state switch
		{
			Interop.UIAutomationClient.ToggleState.ToggleState_Off => ToggleState.Off,
			Interop.UIAutomationClient.ToggleState.ToggleState_On => ToggleState.On,
			Interop.UIAutomationClient.ToggleState.ToggleState_Indeterminate => ToggleState.Indeterminate,
			_ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
		};
	}

	internal static ExpandCollapseState Convert(this Interop.UIAutomationClient.ExpandCollapseState state)
	{
		return state switch
		{
			Interop.UIAutomationClient.ExpandCollapseState.ExpandCollapseState_Collapsed => ExpandCollapseState.Collapsed,
			Interop.UIAutomationClient.ExpandCollapseState.ExpandCollapseState_Expanded => ExpandCollapseState.Expanded,
			Interop.UIAutomationClient.ExpandCollapseState.ExpandCollapseState_PartiallyExpanded => ExpandCollapseState.PartiallyExpanded,
			Interop.UIAutomationClient.ExpandCollapseState.ExpandCollapseState_LeafNode => ExpandCollapseState.LeafNode,
			_ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
		};
	}

	internal static IUIAutomationElement GetCurrentParent(this IUIAutomationElement element)
	{
		return DesktopAutomation.Use(automation =>
		{
			var walker = automation.CreateTreeWalker(automation.RawViewCondition);
			return walker.GetParentElement(element);
		});
	}

	#endregion
}