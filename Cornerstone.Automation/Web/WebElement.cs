#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Automation.Web.Elements;
using Cornerstone.Automation.Internal;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using System.Text.Json.Nodes;

#endregion

namespace Cornerstone.Automation.Web;

public partial class WebElement : SpeedyTree<WebElement>, IElement<WebElement>
{
	#region Fields

	private readonly WebAutomation _automation;
	private readonly JsonObject _element;
	private readonly WebElement _parent;

	/// <summary>
	/// Properties that need to be renamed when requested.
	/// </summary>
	private static readonly Dictionary<string, string> _propertiesToRename;

	#endregion

	#region Constructors

	protected WebElement(JsonNode element, WebAutomation automation, WebElement parent)
		: base(parent)
	{
		_element = element as JsonObject ?? [];
		_automation = automation;
		_parent = parent;

		Values = BuildLookTable(element);
	}

	static WebElement()
	{
		_propertiesToRename = new Dictionary<string, string> { { "class", "className" } };
	}

	#endregion

	#region Properties

	public string[] Classes
	{
		get => _element.GetString("class")?.Split(' ') ?? [];
		set => _element["class"] = string.Join(" ", value);
	}

	public string DisplayIdAndName => GetDisplayName();

	/// <summary>
	/// Gets the optional ID of the frame hosting the element. Will be null if the element is not hosted in a frame.
	/// </summary>
	public string FrameId => _element.GetString("frameId");

	public virtual string Id => _element.GetString("id");

	protected WebAutomation Automation => _automation;

	public bool IsParent { get; set; }

	public string this[string name]
	{
		get => GetAttributeValue(name, false);
		set => SetAttributeValue(name, value);
	}

	public string Name => _element.GetString("name");

	public int Order { get; set; }

	/// <summary>
	/// Gets the ID of the parent element.
	/// </summary>
	public string ParentId => _element.GetString("parentId");

	public Guid? ParentSyncId { get; set; }

	/// <summary>
	/// Gets the tag element name.
	/// </summary>
	public string TagName => _element.GetString("tagName");

	/// <summary>
	/// All values of the element
	/// </summary>
	public IDictionary<string, object> Values { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds a class name to the element
	/// </summary>
	/// <param name="value"> The class name to be written. </param>
	public void AddClass(string value)
	{
		value = WebAutomation.CleanupScriptForJavascriptString(value);

		var script = $"Cornerstone.addClass(\'{Id}\',{GetFrameIdInsert()},\'{value}\')";
		_automation.ExecuteJavaScriptAsync(script);

		var existingClasses = Classes.ToList();
		if (existingClasses.Contains(value))
		{
			return;
		}

		existingClasses.Add(value);
		Classes = existingClasses.ToArray();
		//TriggerElement();
	}

	public static WebElement Create(JsonNode jToken, WebAutomation automation, WebElement parent)
	{
		var tag = jToken.GetString("tagName")?.ToLower();
		if (tag == Elements.Input.HtmlTagName)
		{
			var type = GetTokenAttribute(jToken, "type")?.ToLower();
			return type switch
			{
				"checkbox" => new CheckBox(jToken, automation, parent),
				"radio" => new RadioButton(jToken, automation, parent),
				_ => new Elements.Input(jToken, automation, parent)
			};
		}

		return tag switch
		{
			Button.HtmlTagName => new Button(jToken, automation, parent),
			Form.HtmlTagName => new Form(jToken, automation, parent),
			Link.HtmlTagName => new Link(jToken, automation, parent),
			Select.HtmlTagName => new Select(jToken, automation, parent),
			Option.HtmlTagName => new Option(jToken, automation, parent),
			Table.HtmlTagName => new Table(jToken, automation, parent),
			TextArea.HtmlTagName => new TextArea(jToken, automation, parent),
			_ => new WebElement(jToken, automation, parent)
		};
	}

	public WebElement Click()
	{
		_automation.ExecuteJavaScriptAsync($"document.getElementById(\'{Id}\').click()");
		return this;
	}

	public virtual string Text
	{
		get => this["textContent"];
		set => this["textContent"] = value;
	}

	/// <summary>
	/// Fires an event on the element.
	/// </summary>
	/// <param name="eventName"> The events name to fire. </param>
	/// <param name="eventProperties"> The properties for the event. </param>
	public Task FireEventAsync(string eventName, Dictionary<string, string> eventProperties)
	{
		var values = eventProperties.Aggregate("", (current, item) => current + "{ key: '" + item.Key + "', value: '" + item.Value + "'},");
		if (values.Length > 0)
		{
			values = values.Remove(values.Length - 1, 1);
		}

		var script = $"Cornerstone.triggerEvent(document.getElementById(\'{Id}\'),{GetFrameIdInsert()},\'{eventName.ToLower()}\',[{values}]);";
		return _automation.ExecuteJavaScriptAsync(script);
	}

	/// <summary>
	/// Focuses on the element.
	/// </summary>
	public async Task FocusAsync()
	{
		await _automation.ExecuteJavaScriptAsync($"document.getElementById(\'{Id}\').focus()");
		await FireEventAsync("focus", new Dictionary<string, string>());
	}

	/// <summary>
	/// Gets an attribute value by the provided name.
	/// </summary>
	/// <param name="name"> The name of the attribute to read. </param>
	/// <returns> The attribute value. </returns>
	public string GetAttributeValue(string name)
	{
		return GetAttributeValue(name, _automation.AutoRefresh);
	}

	/// <summary>
	/// Gets an attribute value by the provided name.
	/// </summary>
	/// <param name="name"> The name of the attribute to read. </param>
	/// <param name="refresh"> A flag to force the element to refresh. </param>
	/// <returns> The attribute value. </returns>
	public string GetAttributeValue(string name, bool refresh)
	{
		name = _propertiesToRename.TryGetValue(name, out var renamed) ? renamed : name;

		if (!refresh)
		{
			var cached = GetCachedAttribute(name);
			if (cached != null)
			{
				return cached;
			}

			if ((_automation == null) || !_automation.AutoRefresh)
			{
				return string.Empty;
			}
		}

		var script = $"Cornerstone.getElementValue(\'{Id}\',{GetFrameIdInsert()},\'{name}\')";
		var value = _automation.ExecuteJavaScriptAsync(script).AwaitResults();
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		AddOrUpdateElementAttribute(name, value);
		return value;
	}

	/// <summary>
	/// Gets an attribute style value by the provided name.
	/// </summary>
	/// <param name="name"> The name of the attribute style to read. </param>
	/// <param name="forceRefresh"> A flag to force the element to refresh. </param>
	/// <returns> The attribute style value. </returns>
	public string GetStyleAttributeValue(string name, bool forceRefresh)
	{
		var styleValue = GetAttributeValue("style", forceRefresh);
		if (styleValue == null)
		{
			return string.Empty;
		}

		var styleValues = styleValue.Split(';')
			.Select(x => x.Split(':'))
			.Where(x => x.Length == 2)
			.Select(x => new KeyValuePair<string, string>(x[0].Trim(), x[1].Trim()))
			.ToList()
			.ToDictionary(x => x.Key, x => x.Value);

		return styleValues.TryGetValue(name, out var value) ? value : string.Empty;
	}

	/// <summary>
	/// Highlight  the element.
	/// </summary>
	/// <param name="highlight">
	/// If true the element is highlighted otherwise unhighlighted.
	/// color.
	/// </param>
	public void Highlight(bool highlight)
	{
		//LogManager.Write(highlight ? "Adding highlight to element " + Id + "." : "Removing highlight from element " + Id + ".", LogLevel.Verbose);
		//SetStyleAttributeValue("background-color", highlight ? _highlightColor : _originalColor);

		if (highlight)
		{
			AddClass("cornerstone-highlight");
		}
		else
		{
			RemoveClass("cornerstone-highlight");
		}

		if (_automation.SlowMotion && highlight)
		{
			Delay(150);
		}
	}

	/// <summary>
	/// Remove a class name from the element
	/// </summary>
	/// <param name="value"> The class name to be removed. </param>
	public void RemoveClass(string value)
	{
		value = WebAutomation.CleanupScriptForJavascriptString(value);

		var script = $"Cornerstone.removeClass(\'{Id}\',{GetFrameIdInsert()},\'{value}\')";
		_automation.ExecuteJavaScriptAsync(script);

		var existingClasses = Classes.ToList();
		if (!existingClasses.Contains(value))
		{
			return;
		}

		existingClasses.Remove(value);
		Classes = existingClasses.ToArray();
		//TriggerElement();
	}

	/// <summary>
	/// Sets an attribute value by the provided name.
	/// </summary>
	/// <param name="name"> The name of the attribute to write. </param>
	/// <param name="value"> The value to be written. </param>
	public void SetAttributeValue(string name, string value)
	{
		name = _propertiesToRename.TryGetValue(name, out var value1) ? value1 : name;
		value = WebAutomation.CleanupScriptForJavascriptString(value);

		var script = $"Cornerstone.setElementValue(\'{Id}\',{GetFrameIdInsert()},\'{name}\',\'{value}\')";
		_automation.ExecuteJavaScriptAsync(script);
		AddOrUpdateElementAttribute(name, value);
		//TriggerElement();
	}

	/// <summary>
	/// Sets an attribute style value by the provided name.
	/// </summary>
	/// <param name="name"> The name of the attribute style to write. </param>
	/// <param name="value"> The style value to be written. </param>
	public void SetStyleAttributeValue(string name, string value)
	{
		var styleValue = GetCachedAttribute("style") ?? string.Empty;
		var styleValues = styleValue
			.Split(';')
			.Select(x => x.Split(':'))
			.Where(x => x.Length == 2)
			.Select(x => new KeyValuePair<string, string>(x[0], x[1]))
			.ToList()
			.ToDictionary(x => x.Key, x => x.Value);

		if (!styleValues.ContainsKey(name))
		{
			styleValues.Add(name, value);
		}

		if (string.IsNullOrWhiteSpace(value))
		{
			styleValues.Remove(name);
		}
		else
		{
			styleValues[name] = value;
		}

		styleValue = string.Join(";", styleValues.Select(x => x.Key + ":" + x.Value));
		SetAttributeValue("style", styleValue);
	}

	/// <summary>
	/// Highlight the element.
	/// </summary>
	public void ToggleHighlight()
	{
		Highlight(!Classes.Contains("cornerstone-highlight"));
	}

	internal string GetFrameIdInsert()
	{
		return FrameId == null ? "undefined" : $"\'{FrameId}\'";
	}

	/// <summary>
	/// Add or updates the cached attributes for this element.
	/// </summary>
	/// <param name="name"> </param>
	/// <param name="value"> </param>
	private JsonArray ElementAttributes
	{
		get
		{
			if (_element["attributes"] is JsonArray array)
			{
				return array;
			}

			array = [];
			_element["attributes"] = array;
			return array;
		}
	}

	private void AddOrUpdateElementAttribute(string name, string value)
	{
		var attributes = ElementAttributes;
		for (var i = 0; i < attributes.Count; i += 2)
		{
			var attributeName = attributes[i].AsString();
			if (attributeName == name)
			{
				attributes[i + 1] = value;
				return;
			}
		}

		attributes.Add(name);
		attributes.Add(value);
	}

	private IDictionary<string, object> BuildLookTable(JsonNode element)
	{
		try
		{
			if (element is not JsonObject obj)
			{
				return new Dictionary<string, object>();
			}

			var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			foreach (var pair in obj)
			{
				if (pair.Key == "attributes")
				{
					continue;
				}

				dictionary[pair.Key] = pair.Value.AsString();
			}

			if (obj["attributes"] is JsonArray attributes)
			{
				for (var i = 0; i < attributes.Count; i += 2)
				{
					var key = attributes[i].AsString();
					if (!string.IsNullOrWhiteSpace(key) && ((i + 1) < attributes.Count))
					{
						dictionary.AddOrUpdate(key, attributes[i + 1].AsString());
					}
				}
			}

			return new OrderedDictionary<string, object>(dictionary, StringComparer.OrdinalIgnoreCase);
		}
		catch (Exception)
		{
			return new Dictionary<string, object>();
		}
	}

	/// <summary>
	/// Gets the attribute from the local cache.
	/// </summary>
	/// <param name="name"> The name of the attribute. </param>
	/// <returns> Returns the value or null if the attribute was not found. </returns>
	private string GetCachedAttribute(string name)
	{
		if ((Values != null) && Values.TryGetValue(name, out var mapped) && (mapped != null))
		{
			return mapped.ToString();
		}

		var direct = _element.GetString(name);
		if (direct != null)
		{
			return direct;
		}

		var attributes = ElementAttributes;
		for (var i = 0; i < attributes.Count; i += 2)
		{
			var attributeName = attributes[i].AsString();
			if ((attributeName == name) && ((i + 1) < attributes.Count))
			{
				return attributes[i + 1].AsString();
			}
		}

		return null;
	}

	internal void TriggerElement()
	{
		var libraries = _automation?.JavascriptLibraries;
		if (libraries == null)
		{
			return;
		}

		if (libraries.Contains(JavaScriptLibrary.Angular))
		{
			_automation.ExecuteJavaScriptAsync("angular.element(document.querySelector('#" + Id + "')).triggerHandler('input');");
			_automation.ExecuteJavaScriptAsync("angular.element(document.querySelector('#" + Id + "')).trigger('input');");
			_automation.ExecuteJavaScriptAsync("angular.element(document.querySelector('#" + Id + "')).trigger('change');");
		}

		if (libraries.Contains(JavaScriptLibrary.Vue))
		{
			FireEventAsync("input", new Dictionary<string, string>());
			FireEventAsync("change", new Dictionary<string, string>());
		}
	}

	protected Dictionary<string, string> GetKeyCodeEventProperty(char character)
	{
		return new Dictionary<string, string>
		{
			{ "charCode", ((int) character).ToString() },
			{ "key", character.ToString() },
			{ "keyCode", ((int) character).ToString() },
			{ "which", ((int) character).ToString() }
		};
	}

	private static string GetTokenAttribute(JsonNode token, string name)
	{
		var direct = token.GetString(name);
		if (!string.IsNullOrEmpty(direct))
		{
			return direct;
		}

		if (token is not JsonObject obj || obj["attributes"] is not JsonArray attributes)
		{
			return null;
		}

		for (var i = 0; i < attributes.Count; i += 2)
		{
			if ((attributes[i].AsString() == name) && ((i + 1) < attributes.Count))
			{
				return attributes[i + 1].AsString();
			}
		}

		return null;
	}

	protected void Delay(int milliseconds)
	{
		if (milliseconds <= 0)
		{
			return;
		}

		var provider = _automation?.TimeProvider ?? DateTimeProvider.RealTime;
		if (ReferenceEquals(provider, DateTimeProvider.RealTime))
		{
			Thread.Sleep(milliseconds);
		}
	}

	private string GetDisplayName()
	{
		var builder = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(Id))
		{
			builder.Append(Id);
		}
		if (string.IsNullOrWhiteSpace(Name))
		{
			return builder.ToString();
		}
		if (builder.Length > 0)
		{
			builder.Append(" / ");
		}
		builder.Append(Name);
		return builder.ToString();
	}

	#endregion
}