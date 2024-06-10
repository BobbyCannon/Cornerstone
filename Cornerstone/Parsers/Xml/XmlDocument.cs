#region References

using System.IO;
using System.Text;
using System.Xml;

#endregion

namespace Cornerstone.Parsers.Xml;

public class XmlDocument : XmlElement
{
	#region Constructors

	/// <inheritdoc />
	protected XmlDocument(string name) : base(name)
	{
	}

	#endregion

	#region Properties

	public XmlDeclaration XmlDeclaration { get; set; }

	#endregion

	#region Methods

	public void LoadFile(string projectPath)
	{
		using var reader = File.OpenText(projectPath);
		using var xml = new XmlTextReader(reader);
		LoadXml(xml);
	}

	public void LoadXml(string xml)
	{
		var stream = new StringReader(xml);
		using var reader = new XmlTextReader(stream);
		LoadXml(reader);
	}

	public virtual void LoadXml(XmlTextReader reader)
	{
		Reset();

		reader.Read();

		if (reader.EOF)
		{
			return;
		}

		ProcessReader(reader, this);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		XmlDeclaration = null;
		base.Reset();
	}

	public void SaveFile(string filePath)
	{
		var xml = ToXml();
		File.WriteAllText(filePath, xml, new UTF8Encoding(true));
	}

	public string ToXml()
	{
		var writer = new XmlTextBuilder();
		writer.WriteStartDocument(XmlDeclaration);
		Write(writer);
		writer.WriteEndDocument();
		writer.Flush();
		return writer.ToString();
	}

	protected static void AddAttribute(XmlReader reader, XmlElement element)
	{
		if (reader.NodeType != XmlNodeType.Attribute)
		{
			return;
		}

		var attribute = new XmlAttribute
		{
			Name = reader.Name,
			Value = reader.Value
		};

		element.Attributes.Add(attribute);
	}

	protected virtual XmlElement CreateElement(XmlReader reader)
	{
		return new XmlElement(reader.Name);
	}

	protected XmlElement ProcessReader(XmlTextReader reader)
	{
		var response = CreateElement(reader);
		return ProcessReader(reader, response);
	}

	protected T ProcessReader<T>(XmlTextReader reader, T projectElement)
		where T : XmlElement
	{
		var isEmptyElement = reader.IsEmptyElement;

		ReadAttributes(reader, projectElement);

		if (isEmptyElement)
		{
			return projectElement;
		}

		while (reader.Read())
		{
			switch (reader.NodeType)
			{
				case XmlNodeType.Comment:
				{
					var comment = new XmlComment(reader.Value);
					projectElement.Elements.Add(comment);
					continue;
				}
				case XmlNodeType.Text:
				{
					projectElement.ElementValue = reader.Value;
					continue;
				}
				case XmlNodeType.Element:
				{
					var child = ProcessReader(reader);
					projectElement.Elements.Add(child);
					continue;
				}
				case XmlNodeType.EndElement:
				{
					return projectElement;
				}
			}
		}

		return projectElement;
	}

	protected void ReadAttributes(XmlReader reader, XmlElement element)
	{
		if (reader.MoveToFirstAttribute())
		{
			AddAttribute(reader, element);
		}

		while (reader.MoveToNextAttribute())
		{
			AddAttribute(reader, element);
		}
	}

	#endregion
}