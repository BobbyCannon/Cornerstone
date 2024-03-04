#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for xml.
/// </summary>
public static class XDocumentExtensions
{
	#region Methods

	/// <summary> Retrieves direct child element based on its name. </summary>
	/// <param name="source"> XElement to perform the operation on. </param>
	/// <param name="elementName"> Element name. </param>
	/// <returns> Element if found; otherwise returns null. </returns>
	/// <exception cref="T:System.ArgumentNullException"> <paramref name="source" /> is null. </exception>
	/// <exception cref="T:System.ArgumentException"> <paramref name="elementName" /> is null or empty. </exception>
	public static XElement GetChildElement(this XElement source, string elementName)
	{
		if (source == null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (string.IsNullOrEmpty(elementName))
		{
			throw new ArgumentException("Element name was null or empty.", nameof(elementName));
		}
		return source.Elements().SingleOrDefault(e => e.Name.LocalName == elementName);
	}

	/// <summary>
	/// Retrieves all direct child elements with a particular name.
	/// </summary>
	/// <param name="source"> XElement to perform the operation on. </param>
	/// <param name="elementName"> Element name. </param>
	/// <returns> Collection of elements. </returns>
	/// <exception cref="T:System.ArgumentNullException"> <paramref name="source" /> is null. </exception>
	/// <exception cref="T:System.ArgumentException"> <paramref name="elementName" /> is null or empty. </exception>
	public static IEnumerable<XElement> GetChildElements(this XElement source, string elementName)
	{
		if (source == null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (string.IsNullOrEmpty(elementName))
		{
			throw new ArgumentException("Element name was null or empty.", nameof(elementName));
		}
		return source.Elements().Where(e => e.Name.LocalName == elementName);
	}

	/// <summary> Retrieves a direct child element's value as a string. </summary>
	/// <param name="source"> XElement to perform the operation on. </param>
	/// <param name="elementName"> Element name. </param>
	/// <returns> Element value if found; otherwise null. </returns>
	/// <exception cref="T:System.ArgumentNullException"> <paramref name="source" /> is null. </exception>
	/// <exception cref="T:System.ArgumentException"> <paramref name="elementName" /> is null or empty. </exception>
	public static string GetChildElementValue(this XElement source, string elementName)
	{
		return source.GetChildElement(elementName)?.Value;
	}

	/// <summary>
	/// Determines if the documents root element's name is equal to <paramref name="rootName" />.
	/// </summary>
	/// <param name="source"> XDocument to perform the operation on. </param>
	/// <param name="rootName"> Root element name. </param>
	/// <returns> True if root element name is equal; otherwise false. </returns>
	/// <exception cref="T:System.ArgumentNullException"> <paramref name="source" /> is null. </exception>
	/// <exception cref="T:System.ArgumentException"> <paramref name="rootName" /> is null or empty. </exception>
	public static bool IsRootName(this XDocument source, string rootName)
	{
		if (source == null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (string.IsNullOrEmpty(rootName))
		{
			throw new ArgumentException("Root name was null or empty.", nameof(rootName));
		}
		return source.Root?.Name.LocalName == rootName;
	}

	/// <summary>
	/// Convert the element to the original XML.
	/// </summary>
	/// <param name="element"> The element to get the XML for. </param>
	/// <param name="removeXmlnsAttribute"> Remove the [xmlns] from the XML. </param>
	/// <returns> The xml format of the element. </returns>
	public static string ToXml(this XElement element, bool removeXmlnsAttribute = true)
	{
		var reader = element?.CreateReader(ReaderOptions.OmitDuplicateNamespaces);
		reader?.MoveToContent();
		var response = reader?.ReadOuterXml() ?? string.Empty;
		return removeXmlnsAttribute
			? response.Replace(" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"", "")
			: response;
	}

	#endregion
}