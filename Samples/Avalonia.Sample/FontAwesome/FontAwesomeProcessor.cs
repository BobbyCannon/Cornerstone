#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Cornerstone.Extensions;

#endregion

namespace Avalonia.Sample.FontAwesome;

/// <summary>
/// This application is to generate code for things like FontAwesome
/// </summary>
public static class FontAwesomeProcessor
{
	#region Fields

	private static readonly StringBuilder _content;
	private static readonly Stack<string> _indent;

	#endregion

	#region Constructors

	static FontAwesomeProcessor()
	{
		_content = new StringBuilder();
		_indent = new Stack<string>();
	}

	#endregion

	#region Methods

	public static void GenerateAvaloniaStyles(FontAwesomeManager manager, string outputDirectory)
	{
		var outputFile = Path.Combine(outputDirectory, "FontAwesomeIcons.axaml");

		_content.Clear();
		_indent.Clear();

		WriteLine("<!-- This code was generated. Please do not modify. -->");
		WriteLine("<ResourceDictionary xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
		PushIndent("\t");

		var textInfo = new CultureInfo("en-US", false).TextInfo;
		var keys = manager.IconLookup.Keys.OrderBy(x => x);

		foreach (var key in keys)
		{
			var icon = manager.IconLookup[key];
			var name = key.Replace("-", ".");

			if (icon.Svg.Brands != null)
			{
				WriteLine($"<StreamGeometry x:Key=\"FontAwesome.{textInfo.ToTitleCase(name)}.Brands\">{icon.Svg.Brands.Path}</StreamGeometry>");
			}

			if (icon.Svg.Light != null)
			{
				WriteLine($"<StreamGeometry x:Key=\"FontAwesome.{textInfo.ToTitleCase(name)}.Light\">{icon.Svg.Light.Path}</StreamGeometry>");
			}

			if (icon.Svg.Regular != null)
			{
				WriteLine($"<StreamGeometry x:Key=\"FontAwesome.{textInfo.ToTitleCase(name)}.Regular\">{icon.Svg.Regular.Path}</StreamGeometry>");
			}

			if (icon.Svg.Solid != null)
			{
				WriteLine($"<StreamGeometry x:Key=\"FontAwesome.{textInfo.ToTitleCase(name)}.Solid\">{icon.Svg.Solid.Path}</StreamGeometry>");
			}
		}

		PopIndent();
		WriteLine("</ResourceDictionary>");

		var fileInfo = new FileInfo(outputFile);
		var directoryInfo = new DirectoryInfo(fileInfo.DirectoryName);
		directoryInfo.SafeCreate();

		Console.WriteLine(fileInfo.FullName);
		File.WriteAllText(fileInfo.FullName, _content.ToString());
	}

	private static string GetIndent()
	{
		var indent = "";
		foreach (var entry in _indent)
		{
			indent += entry;
		}

		return indent;
	}

	private static void PopIndent()
	{
		_indent.Pop();
	}

	private static void PushIndent(string indent)
	{
		_indent.Push(indent);
	}

	private static void Write(string text)
	{
		_content.Append(GetIndent() + text);
	}

	private static void WriteLine(string text)
	{
		_content.AppendLine(GetIndent() + text);
	}

	private static void WriteLine(string text, params object[] parameter)
	{
		_content.AppendLine(GetIndent() + string.Format(text, parameter));
	}

	private static void WriteSummary(string text)
	{
		WriteLine("/// <summary>");
		WriteLine("/// {0}", text);
		WriteLine("/// </summary>");
	}

	#endregion
}