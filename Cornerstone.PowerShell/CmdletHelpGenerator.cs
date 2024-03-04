#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Xml;
using Cornerstone.PowerShell.Documentation;
using Cornerstone.Text;

#endregion

namespace Cornerstone.PowerShell;

public static class CmdletHelpGenerator
{
	#region Fields

	private static readonly string _allParameterSetsName;

	#endregion

	#region Constructors

	static CmdletHelpGenerator()
	{
		_allParameterSetsName = "__AllParameterSets";
	}

	#endregion

	#region Methods

	public static string GenerateMarkdown(Assembly assembly)
	{
		var writer = new TextBuilder();
		var groupedCmdlets = new Dictionary<string, SortedList<string, CmdletDetails>>();

		foreach (var type in assembly.GetExportedTypes())
		{
			var ca = GetAttribute<CmdletAttribute>(type);
			if (ca == null)
			{
				continue;
			}

			var details = new CmdletDetails
			{
				Type = type,
				CmdletAttribute = ca,
				CmdletDescriptionAttribute = GetAttribute<CmdletDescriptionAttribute>(type),
				CmdletGroupAttribute = GetAttribute<CmdletGroupAttribute>(type),
				CmdletExampleAttributes = GetAttributes<CmdletExampleAttribute>(type)
			};

			foreach (var pi in type.GetProperties())
			{
				var pas = GetAttribute<ParameterAttribute>(pi);
				if (pas == null)
				{
					continue;
				}

				details.ParameterAttributes.Add(pi, pas.FirstOrDefault());
			}

			var group = details.CmdletGroupAttribute?.Group ?? string.Empty;
			if (!groupedCmdlets.ContainsKey(group))
			{
				groupedCmdlets.Add(group, new SortedList<string, CmdletDetails>());
			}

			groupedCmdlets[group].Add($"{ca.VerbName}-{ca.NounName}", details);
		}

		writer.AppendLine("### Table of Contents {#TableOfContents}");
		writer.AppendLine();

		foreach (var group in groupedCmdlets.OrderBy(x => x.Key))
		{
			writer.AppendLine($"* [{group.Key}](#{group.Key})");

			foreach (var type in group.Value)
			{
				writer.AppendLine($"  * [{type.Key}](#{type.Key})");
			}
		}

		foreach (var group in groupedCmdlets.OrderBy(x => x.Key))
		{
			writer.AppendLine();
			writer.AppendLine($"# {group.Key} {{#{group.Key}}}");

			foreach (var pair in group.Value)
			{
				writer.AppendLine();
				writer.AppendLine($"#### {pair.Key} {{#{pair.Key}}} ");
				writer.AppendLine();

				var details = pair.Value;
				if (details.CmdletDescriptionAttribute != null)
				{
					writer.AppendLine(details.CmdletDescriptionAttribute.Synopsis);
				}

				// Syntax
				writer.AppendLine();
				writer.AppendLine("```");
				writer.AppendLine($"{details.CmdletAttribute.VerbName}-{details.CmdletAttribute.NounName}");

				foreach (var p in details.ParameterAttributes
							.OrderByDescending(x => x.Value.Mandatory)
							.ThenBy(x => x.Value.Position)
							.ThenBy(x => x.Key.Name))
				{
					writer.Append(p.Value.Mandatory ? "\t" : "\t[");
					writer.Append($"-{p.Key.Name}");
					writer.Append(p.Value.Mandatory ? "" : "]");

					if (p.Key.PropertyType.Name != "SwitchParameter")
					{
						writer.Append($" <{p.Key.PropertyType.Name}>");
					}

					writer.AppendLine();
				}

				if (pair.Value.CmdletAttribute.ConfirmImpact == ConfirmImpact.High)
				{
					writer.AppendLine("\t[-Confirm]");
				}

				if (pair.Value.CmdletAttribute.SupportsShouldProcess)
				{
					writer.AppendLine("\t[-WhatIf]");
				}

				writer.AppendLine("\t[<CommonParameters>]");
				writer.AppendLine("```");

				// Description
				if ((details.CmdletDescriptionAttribute != null) && (details.CmdletDescriptionAttribute.Synopsis != details.CmdletDescriptionAttribute.Description) && !string.IsNullOrWhiteSpace(pair.Value.CmdletDescriptionAttribute.Description))
				{
					writer.AppendLine();
					writer.AppendLine(pair.Value.CmdletDescriptionAttribute.Description);
					writer.AppendLine();
				}

				// Parameters
				foreach (var p in details.ParameterAttributes
							.OrderByDescending(x => x.Value.Mandatory)
							.ThenBy(x => x.Value.Position)
							.ThenBy(x => x.Key.Name))
				{
					writer.AppendLine("");
					writer.AppendLine($"-{p.Key.Name}");
					writer.AppendLine();
					writer.AppendLine($"{p.Value.HelpMessage}");
					writer.AppendLine();

					writer.AppendLine("|||");
					writer.AppendLine("|-|-|");
					writer.AppendLine($"|Type|{p.Key.PropertyType.Name}|");

					//writer.Append($"|Aliases|{p.Key.PropertyType.Name}|");
					if (p.Value.ValueFromPipelineByPropertyName)
					{
						writer.AppendLine("|Position|Named|");
						writer.AppendLine("|Accepts pipeline input|True (ByPropertyName)|");
					}
					else if (p.Value.ValueFromPipeline)
					{
						writer.AppendLine($"|Position|{p.Value.Position}|");
						writer.AppendLine("|Accepts pipeline input|True (ByValue)|");
					}
					else
					{
						writer.AppendLine("|Accepts pipeline input|False|");
					}
				}

				// Examples
				var orderedExamples = details.CmdletExampleAttributes.OrderBy(x => x.Order).ToList();
				if (orderedExamples.Any())
				{
					for (var i = 0; i < orderedExamples.Count; i++)
					{
						var e = orderedExamples[i];
						writer.AppendLine("");
						writer.AppendLine("###### " + (orderedExamples.Count == 1 ? "Example" : $"Example {i + 1}"));
						writer.AppendLine(e.Remarks);
						writer.AppendLine("``` PowerShell");
						writer.AppendLine(e.Code);
						writer.AppendLine("```");
					}
				}

				writer.AppendLine();
				writer.AppendLine("[back to top](#TableOfContents)");
				writer.AppendLine("<br />");
			}
		}

		return writer.ToString();
	}

	public static string GenerateXml(Assembly assembly)
	{
		var sb = new StringBuilder();
		var writer = new XmlTextWriter(new StringWriter(sb)) { Formatting = Formatting.Indented, IndentChar = '\t', Indentation = 1 };
		var exportedTypes = assembly.GetExportedTypes()
			.OrderBy(x => x.Name)
			.ToList();

		writer.WriteStartElement("helpItems");
		writer.WriteAttributeString("xmlns", "http://msh");
		writer.WriteAttributeString("schema", "maml");

		foreach (var type in exportedTypes)
		{
			var ca = GetAttribute<CmdletAttribute>(type);
			if (ca == null)
			{
				continue;
			}

			Console.WriteLine("Found Cmdlet: {0}-{1}", ca.VerbName, ca.NounName);

			writer.WriteStartElement("command", "command", "http://schemas.microsoft.com/maml/dev/command/2004/10");
			writer.WriteAttributeString("xmlns", "maml", null, "http://schemas.microsoft.com/maml/2004/1");
			writer.WriteAttributeString("xmlns", "dev", null, "http://schemas.microsoft.com/maml/dev/2004/10");
			writer.WriteAttributeString("xmlns", "gl", null, "http://schemas.falchionconsulting.com/maml/gl/2013/02");

			writer.WriteStartElement("command", "details", null);
			writer.WriteElementString("command", "name", null, $"{ca.VerbName}-{ca.NounName}");

			var group = GetAttribute<CmdletGroupAttribute>(type);
			writer.WriteElementString("gl", "group", null, !string.IsNullOrEmpty(group?.Group) ? group.Group : ca.NounName);

			WriteDescription(type, true, writer);

			writer.WriteElementString("command", "verb", null, ca.VerbName);
			writer.WriteElementString("command", "noun", null, ca.NounName);

			//writer.WriteElementString("dev", "version", null, asm.GetDisplayName().Version.ToString());

			writer.WriteEndElement(); //command:details

			WriteDescription(type, true, writer);
			WriteSyntax(ca, type, writer);

			writer.WriteStartElement("command", "parameters", null);

			foreach (var pi in type.GetProperties())
			{
				var pas = GetAttribute<ParameterAttribute>(pi);
				if (pas == null)
				{
					continue;
				}

				ParameterAttribute pa = null;
				ParameterAttribute defaultPa = null;

				if (pas.Count == 1)
				{
					pa = pas[0];
				}
				else
				{
					// Determine the default property parameter set to use for details.
					foreach (var temp in pas)
					{
						var defaultSet = ca.DefaultParameterSetName;
						if (string.IsNullOrEmpty(ca.DefaultParameterSetName))
						{
							defaultSet = string.Empty;
						}

						var set = temp.ParameterSetName;
						if (string.IsNullOrEmpty(set) || (set == _allParameterSetsName))
						{
							set = string.Empty;
							defaultPa = temp;
						}

						if (string.Equals(set, defaultSet, StringComparison.OrdinalIgnoreCase))
						{
							pa = temp;
							defaultPa = temp;
							break;
						}
					}

					if ((pa == null) && (defaultPa != null))
					{
						pa = defaultPa;
					}

					if (pa == null)
					{
						pa = pas[0];
					}
				}

				writer.WriteStartElement("command", "parameter", null);
				writer.WriteAttributeString("required", pa.Mandatory.ToString().ToLower());

				var supportsWildcard = GetAttribute<CmdletSupportsWildcardsAttribute>(pi) != null;
				writer.WriteAttributeString("globbing", supportsWildcard.ToString().ToLower());

				if (!pa.ValueFromPipeline && !pa.ValueFromPipelineByPropertyName)
				{
					writer.WriteAttributeString("pipelineInput", "false");
				}
				else if (pa.ValueFromPipeline && pa.ValueFromPipelineByPropertyName)
				{
					writer.WriteAttributeString("pipelineInput", "true (ByValue, ByPropertyName)");
				}
				else if (!pa.ValueFromPipeline && pa.ValueFromPipelineByPropertyName)
				{
					writer.WriteAttributeString("pipelineInput", "true (ByPropertyName)");
				}
				else if (pa.ValueFromPipeline && !pa.ValueFromPipelineByPropertyName)
				{
					writer.WriteAttributeString("pipelineInput", "true (ByValue)");
				}

				writer.WriteAttributeString("position", pa.Position < 0 ? "named" : (pa.Position + 1).ToString());

				var variableLength = pi.PropertyType.IsArray;
				writer.WriteAttributeString("variableLength", variableLength.ToString().ToLower());
				writer.WriteElementString("maml", "name", null, pi.Name);

				if (pi.PropertyType.Name == "SPAssignmentCollection")
				{
					WriteSpAssignmentCollectionDescription(writer);
				}
				else
				{
					WriteDescription(pa.HelpMessage, writer);
				}

				writer.WriteStartElement("command", "parameterValue", null);
				writer.WriteAttributeString("required", pa.Mandatory.ToString().ToLower());
				writer.WriteAttributeString("variableLength", variableLength.ToString().ToLower());
				writer.WriteValue(pi.PropertyType.Name);
				writer.WriteEndElement(); //command:parameterValue

				WriteDevType(pi.PropertyType.Name, null, writer);

				writer.WriteEndElement(); //command:parameter
			}

			writer.WriteEndElement(); //command:parameters

			//TODO: Find out what is supposed to go here
			writer.WriteStartElement("command", "inputTypes", null);
			writer.WriteStartElement("command", "inputType", null);
			WriteDevType(null, null, writer);
			writer.WriteEndElement(); //command:inputType
			writer.WriteEndElement(); //command:inputTypes

			writer.WriteStartElement("command", "returnValues", null);
			writer.WriteStartElement("command", "returnValue", null);
			WriteDevType(null, null, writer);
			writer.WriteEndElement(); //command:returnValue
			writer.WriteEndElement(); //command:returnValues

			writer.WriteElementString("command", "terminatingErrors", null, null);
			writer.WriteElementString("command", "nonTerminatingErrors", null, null);

			writer.WriteStartElement("maml", "alertSet", null);
			writer.WriteElementString("maml", "title", null, null);
			writer.WriteStartElement("maml", "alert", null);
			WritePara(string.Format("For more information, type \"Get-Help {0}-{1} -detailed\". For technical information, type \"Get-Help {0}-{1} -full\".", ca.VerbName, ca.NounName), writer);
			writer.WriteEndElement(); //maml:alert
			writer.WriteEndElement(); //maml:alertSet

			WriteExamples(type, writer);
			WriteRelatedLinks(type, writer, ca);

			writer.WriteEndElement(); //command:command
		}

		writer.WriteEndElement(); //helpItems
		writer.Flush();

		return sb.ToString();
	}

	private static T GetAttribute<T>(Type type)
	{
		var attrs = type.GetCustomAttributes(typeof(T), true);
		if (attrs.Length == 0)
		{
			return default;
		}

		return (T) attrs[0];
	}

	private static List<T> GetAttribute<T>(PropertyInfo pi)
	{
		var attrs = pi.GetCustomAttributes(typeof(T), true);
		var attributes = new List<T>();
		if (attrs.Length == 0)
		{
			return null;
		}

		foreach (T t in attrs)
		{
			attributes.Add(t);
		}

		return attributes;
	}

	private static List<T> GetAttributes<T>(Type type)
	{
		return type.GetCustomAttributes(typeof(T), true).Cast<T>().ToList();
	}

	private static ParameterAttribute GetParameterAttribute(PropertyInfo pi, string parameterSetName)
	{
		var pas = GetAttribute<ParameterAttribute>(pi);
		if (pas == null)
		{
			return null;
		}

		ParameterAttribute pa = null;
		foreach (var temp in pas)
		{
			if (temp.ParameterSetName.ToLower() == parameterSetName.ToLower())
			{
				pa = temp;
				break;
			}
		}

		return pa;
	}

	private static void WriteDescription(Type type, bool synopsis, XmlTextWriter writer)
	{
		writer.WriteStartElement("maml", "description", null);

		var da = GetAttribute<CmdletDescriptionAttribute>(type);
		var desc = string.Empty;
		if (synopsis)
		{
			if ((da != null) && !string.IsNullOrEmpty(da.Synopsis))
			{
				desc = da.Synopsis;
			}
		}
		else
		{
			if ((da != null) && !string.IsNullOrEmpty(da.Description))
			{
				desc = da.Description;
			}
		}

		WritePara(desc, writer);

		writer.WriteEndElement(); //maml:description
	}

	private static void WriteDescription(string desc, XmlTextWriter writer)
	{
		writer.WriteStartElement("maml", "description", null);

		WritePara(desc, writer);

		writer.WriteEndElement(); //maml:description
	}

	private static void WriteDevType(string name, string description, XmlTextWriter writer)
	{
		writer.WriteStartElement("dev", "type", null);
		writer.WriteElementString("maml", "name", null, name);
		writer.WriteElementString("maml", "uri", null, null);

		WriteDescription(description, writer);

		writer.WriteEndElement(); //dev:type
	}

	private static void WriteExamples(Type type, XmlTextWriter writer)
	{
		var attrs = type.GetCustomAttributes(typeof(CmdletExampleAttribute), true)
			.Cast<CmdletExampleAttribute>()
			.OrderBy(x => x.Order)
			.ThenBy(x => x.Code)
			.ToList();

		if (attrs.Count == 0)
		{
			writer.WriteElementString("command", "examples", null, null);
			return;
		}

		writer.WriteStartElement("command", "examples", null);

		for (var i = 0; i < attrs.Count; i++)
		{
			var ex = attrs[i];

			writer.WriteStartElement("command", "example", null);
			writer.WriteElementString("maml", "title", null, attrs.Count == 1 ? "------------------EXAMPLE------------------" : $"------------------EXAMPLE {i + 1}-----------------------");
			writer.WriteElementString("dev", "code", null, ex.Code);
			writer.WriteStartElement("dev", "remarks", null);

			WritePara(ex.Remarks, writer);

			writer.WriteEndElement(); //dev:remarks
			writer.WriteEndElement(); //command:example
		}

		writer.WriteEndElement(); //command:examples
	}

	private static void WritePara(string para, XmlTextWriter writer)
	{
		if (string.IsNullOrEmpty(para))
		{
			writer.WriteElementString("maml", "para", null, null);
			return;
		}

		var paragraphs = para.Split(new[] { "\r\n" }, StringSplitOptions.None);
		foreach (var p in paragraphs)
		{
			writer.WriteElementString("maml", "para", null, p);
		}
	}

	private static void WriteParameter(PropertyInfo pi, ParameterAttribute pa, XmlTextWriter writer)
	{
		writer.WriteStartElement("command", "parameter", null);
		writer.WriteAttributeString("required", pa.Mandatory.ToString().ToLower());
		//writer.WriteAttributeString("parameterSetName", pa.ParameterSetName);
		if (pa.Position < 0)
		{
			writer.WriteAttributeString("position", "named");
		}
		else
		{
			writer.WriteAttributeString("position", (pa.Position + 1).ToString());
		}

		writer.WriteElementString("maml", "name", null, pi.Name);
		writer.WriteStartElement("command", "parameterValue", null);

		if (pi.DeclaringType == typeof(PSCmdlet))
		{
			writer.WriteAttributeString("required", "false");
		}
		else
		{
			writer.WriteAttributeString("required", "true");
		}

		if (pi.PropertyType.Name == "Nullable`1")
		{
			var coreType = pi.PropertyType.GetGenericArguments()[0];
			if (coreType.IsEnum)
			{
				writer.WriteValue(string.Join(" | ", Enum.GetNames(coreType)));
			}
			else
			{
				writer.WriteValue(coreType.Name);
			}
		}
		else
		{
			if (pi.PropertyType.IsEnum)
			{
				writer.WriteValue(string.Join(" | ", Enum.GetNames(pi.PropertyType)));
			}
			else
			{
				writer.WriteValue(pi.PropertyType.Name);
			}
		}

		writer.WriteEndElement(); //command:parameterValue
		writer.WriteEndElement(); //command:parameter
	}

	private static void WriteRelatedLinks(Type type, XmlTextWriter writer, CmdletAttribute attribute)
	{
		var attr = GetAttribute<CmdletsRelatedAttribute>(type);

		writer.WriteStartElement("maml", "relatedLinks", null);
		writer.WriteStartElement("maml", "navigationLink", null);
		writer.WriteElementString("maml", "linkText", null, "Online version:");
		writer.WriteElementString("maml", "uri", null, attribute.HelpUri);
		writer.WriteEndElement(); //maml:navigationLink

		if (attr != null)
		{
			foreach (var t in attr.RelatedCmdlets)
			{
				var ca = GetAttribute<CmdletAttribute>(t);
				if (ca == null)
				{
					continue;
				}

				writer.WriteStartElement("maml", "navigationLink", null);
				writer.WriteElementString("maml", "linkText", null, ca.VerbName + "-" + ca.NounName);
				writer.WriteElementString("maml", "uri", null, null);
				writer.WriteEndElement(); //maml:navigationLink
			}

			if (attr.ExternalCmdlets != null)
			{
				foreach (var s in attr.ExternalCmdlets)
				{
					writer.WriteStartElement("maml", "navigationLink", null);
					writer.WriteElementString("maml", "linkText", null, s);
					writer.WriteElementString("maml", "uri", null, null);
					writer.WriteEndElement(); //maml:navigationLink
				}
			}
		}

		writer.WriteEndElement(); //maml:relatedLinks
	}

	private static void WriteSpAssignmentCollectionDescription(XmlTextWriter writer)
	{
		WriteDescription("Manages objects for the purpose of proper disposal. Use of objects, such as SPWeb or SPSite, can use large amounts of memory and use of these objects in Windows PowerShell scripts requires proper memory management. Using the SPAssignment object, you can assign objects to a variable and dispose of the objects after they are needed to free up memory. When SPWeb, SPSite, or SPSiteAdministration objects are used, the objects are automatically disposed of if an assignment collection or the Global parameter is not used.\r\n\r\nWhen the Global parameter is used, all objects are contained in the global store. If objects are not immediately used, or disposed of by using the Stop-SPAssignment command, an out-of-memory scenario can occur.", writer);
	}

	private static void WriteSyntax(CmdletAttribute ca, Type type, XmlTextWriter writer)
	{
		var parameterSets = new Dictionary<string, List<PropertyInfo>>();

		List<PropertyInfo> allParameterSets = null;
		foreach (var pi in type.GetProperties())
		{
			var pas = GetAttribute<ParameterAttribute>(pi);
			if (pas == null)
			{
				continue;
			}

			foreach (var temp in pas)
			{
				var set = temp.ParameterSetName + "";
				List<PropertyInfo> piList = null;
				if (!parameterSets.ContainsKey(set))
				{
					piList = new List<PropertyInfo>();
					parameterSets.Add(set, piList);
				}
				else
				{
					piList = parameterSets[set];
				}

				parameterSets[set].Add(pi);
			}
		}

		if (parameterSets.Count == 0)
		{
			return;
		}

		if (parameterSets.TryGetValue(_allParameterSetsName, out var parameterSet))
		{
			allParameterSets = parameterSet;
		}

		if ((parameterSets.Count > 1) && parameterSets.ContainsKey(_allParameterSetsName))
		{
			parameterSets.Remove(_allParameterSetsName);
		}

		if (parameterSets.Count == 1)
		{
			allParameterSets = null;
		}

		writer.WriteStartElement("command", "syntax", null);
		foreach (var parameterSetName in parameterSets.Keys)
		{
			WriteSyntaxItem(ca, parameterSets, parameterSetName, allParameterSets, writer);
		}

		writer.WriteEndElement(); //command:syntax
	}

	private static void WriteSyntaxItem(CmdletAttribute ca, Dictionary<string, List<PropertyInfo>> parameterSets, string parameterSetName, List<PropertyInfo> allParameterSets, XmlTextWriter writer)
	{
		writer.WriteStartElement("command", "syntaxItem", null);
		writer.WriteElementString("maml", "name", null, $"{ca.VerbName}-{ca.NounName}");
		foreach (var pi in parameterSets[parameterSetName])
		{
			var pa = GetParameterAttribute(pi, parameterSetName);
			if (pa == null)
			{
				continue;
			}

			WriteParameter(pi, pa, writer);
		}

		if (allParameterSets != null)
		{
			foreach (var pi in allParameterSets)
			{
				var pas = GetAttribute<ParameterAttribute>(pi);
				if (pas == null)
				{
					continue;
				}

				WriteParameter(pi, pas[0], writer);
			}
		}

		writer.WriteEndElement(); //command:syntaxItem
	}

	#endregion

	#region Classes

	private class CmdletDetails
	{
		#region Constructors

		public CmdletDetails()
		{
			CmdletExampleAttributes = new List<CmdletExampleAttribute>();
			ParameterAttributes = new Dictionary<PropertyInfo, ParameterAttribute>();
		}

		#endregion

		#region Properties

		public CmdletAttribute CmdletAttribute { get; set; }

		public CmdletDescriptionAttribute CmdletDescriptionAttribute { get; set; }

		public List<CmdletExampleAttribute> CmdletExampleAttributes { get; set; }

		public CmdletGroupAttribute CmdletGroupAttribute { get; set; }

		public IDictionary<PropertyInfo, ParameterAttribute> ParameterAttributes { get; }

		public Type Type { get; set; }

		#endregion
	}

	#endregion
}