#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Parsers.Xml;

public class XmlElement : Notifiable
{
	#region Constructors

	public XmlElement() : this(string.Empty)
	{
	}

	public XmlElement(string elementName)
	{
		ElementName = elementName;
		Attributes = new List<XmlAttribute>();
		Elements = new List<XmlElement>();
	}

	#endregion

	#region Properties

	public IList<XmlAttribute> Attributes { get; set; }

	public string ElementName { get; set; }

	public IList<XmlElement> Elements { get; set; }

	public string ElementValue { get; set; }

	#endregion

	#region Methods

	public string GetAttributeOrElementValue(string name)
	{
		return GetAttributeValue(name) ?? GetElementValue(name);
	}

	public string GetAttributeValue(string name)
	{
		return Attributes?.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
	}

	public string GetElementValue(string name)
	{
		return Elements?.FirstOrDefault(x => string.Equals(x.ElementName, name, StringComparison.OrdinalIgnoreCase))?.ElementValue;
	}

	public virtual void Reset()
	{
		ElementName = null;
		ElementValue = null;
		Attributes.Clear();
		Elements.Clear();
	}

	public bool SetAttributeOrElementValue(string name, string value)
	{
		return TrySetAttributeValue(name, value)
			|| TrySetElementValue(name, value);
	}

	public void SetOrAddAttributeValue(string name, string value)
	{
		if (TrySetAttributeValue(name, value))
		{
			return;
		}

		Attributes.Add(new XmlAttribute(name, value));
	}

	public bool TrySetAttributeValue(string name, string value)
	{
		var attribute = Attributes.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
		if (attribute == null)
		{
			return false;
		}
		attribute.Value = value;
		return true;
	}

	public bool TrySetElementValue(string name, string value)
	{
		var element = Elements.FirstOrDefault(x => string.Equals(x.ElementName, name, StringComparison.OrdinalIgnoreCase));
		if (element == null)
		{
			return false;
		}
		element.ElementValue = value;
		return true;
	}

	/// <summary>
	/// Update the XmlElement with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(XmlElement update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <summary>
	/// Update the XmlElement with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(XmlElement update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith - XmlElement

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(Attributes, update.Attributes, settings.ShouldProcessProperty(nameof(Attributes)), x => Attributes = x);
		UpdateProperty(ElementName, update.ElementName, settings.ShouldProcessProperty(nameof(ElementName)), x => ElementName = x);
		UpdateProperty(Elements, update.Elements, settings.ShouldProcessProperty(nameof(Elements)), x => Elements = x);
		UpdateProperty(ElementValue, update.ElementValue, settings.ShouldProcessProperty(nameof(ElementValue)), x => ElementValue = x);

		// Code Generated - /UpdateWith - XmlElement

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			XmlElement value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	/// <summary>
	/// Parses version then normalizes to match nuget.
	/// </summary>
	/// <param name="versionString"> The version string. </param>
	/// <returns> Returns Version if found otherwise returns null. </returns>
	protected Version ParseVersionOrDefault(string versionString)
	{
		if (!Version.TryParse(versionString, out var parsedVersion))
		{
			return null;
		}

		var normalized = parsedVersion;

		if ((parsedVersion.Build < 0)
			|| (parsedVersion.Revision < 0))
		{
			normalized = new Version(
				parsedVersion.Major,
				parsedVersion.Minor,
				Math.Max(parsedVersion.Build, 0),
				Math.Max(parsedVersion.Revision, 0));
		}

		return normalized;
	}

	protected virtual void Write(XmlTextBuilder writer)
	{
		writer.WriteStartElement(ElementName);

		foreach (var projectAttribute in Attributes)
		{
			writer.WriteAttributeString(projectAttribute.Name, projectAttribute.Value);
		}

		foreach (var element in Elements)
		{
			element.Write(writer);
		}

		if (ElementValue != null)
		{
			writer.WriteString(ElementValue);
		}

		writer.WriteEndElement();
	}

	#endregion
}