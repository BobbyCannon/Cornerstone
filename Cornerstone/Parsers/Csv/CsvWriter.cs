#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cornerstone.Collections;
using Cornerstone.Convert;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Parsers.Csv;

/// <summary>
/// Converts a list of items to CSV format.
/// </summary>
public class CsvWriter<T> : CsvWriter
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the CSV writer.
	/// </summary>
	/// <param name="settings"> The options for writing. </param>
	public CsvWriter(CsvConverterSettings settings) : base(settings, typeof(T))
	{
	}

	#endregion
}

/// <summary>
/// Converts a list of items to CSV format.
/// </summary>
public class CsvWriter
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the CSV writer.
	/// </summary>
	/// <param name="settings"> The options for writing. </param>
	/// <param name="type"> The type this writer is for </param>
	public CsvWriter(CsvConverterSettings settings, Type type)
		: this(settings, GetTypeHeaders(type).Select(x => x.Name).ToArray())
	{
	}

	/// <summary>
	/// Initializes a new instance of the CSV writer.
	/// </summary>
	/// <param name="settings"> The options for writing. </param>
	/// <param name="headers"> The headers for the file. </param>
	public CsvWriter(CsvConverterSettings settings, params string[] headers)
	{
		Settings = settings;
		Headers = new ReadOnlySet<string>(headers);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The list of fields (headers).
	/// </summary>
	public ReadOnlySet<string> Headers { get; }

	/// <summary>
	/// The options for writing the CSV.
	/// </summary>
	public CsvConverterSettings Settings { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Converts a value to how it should output in a csv file
	/// If it has a comma, it needs surrounding with double quotes
	/// Eg Sydney, Australia -> "Sydney, Australia"
	/// Also if it contains any double quotes ("), then they need to be replaced with quad quotes[sic] ("")
	/// Eg "Dangerous Dan" McGrew -> """Dangerous Dan"" McGrew"
	/// </summary>
	/// <param name="value"> The value to escape. </param>
	/// <param name="settings"> The options for writing. </param>
	public static string EscapeValue(object value, CsvConverterSettings settings)
	{
		var output = value.ConvertTo<string>(settings);

		if ((settings.FieldLimit > 0) && (output.Length > settings.FieldLimit))
		{
			output = output.Substring(0, settings.FieldLimit);
		}

		#if (NETSTANDARD)
		if (output.Contains(settings.Delimiter)
			|| output.Contains("\"")
			|| output.Contains("\n")
			|| output.Contains("\r"))
		{
			output = $"\"{output.Replace("\"", "\"\"")}\"";
		}
		#else
		if (output.Contains(settings.Delimiter)
			|| output.Contains('\"')
			|| output.Contains('\n')
			|| output.Contains('\r'))
		{
			output = $"\"{output.Replace("\"", "\"\"")}\"";
		}
		#endif

		return output;
	}

	/// <summary>
	/// Write a small set of items to a memory string.
	/// </summary>
	/// <typeparam name="T"> The type of the values. </typeparam>
	/// <param name="writer"> The writer to write to. </param>
	/// <param name="settings"> The options for writing. </param>
	/// <param name="values"> The values to write. </param>
	public static void Write<T>(TextWriter writer, CsvConverterSettings settings, params T[] values)
	{
		var headerValues = GetTypeHeaders(typeof(T));
		var line = CreateLine(settings, headerValues.Select(x => x.Name).ToArray());
		writer.WriteLine(line);

		foreach (var value in values)
		{
			line = CreateLine(settings, GetObjectValues(value, headerValues));
			writer.WriteLine(line);
		}
	}

	/// <summary>
	/// Write the header line to the writer provided.
	/// </summary>
	/// <param name="writer"> The writer to write to. </param>
	public void WriteHeaders(TextWriter writer)
	{
		var line = CreateLine(Settings, Headers);
		writer.WriteLine(line);
	}

	/// <summary>
	/// Write a line to the writer provided.
	/// </summary>
	/// <param name="writer"> The writer to write to. </param>
	/// <param name="values"> The values to write. </param>
	public void WriteLine(TextWriter writer, params object[] values)
	{
		var line = CreateLine(Settings, values);
		writer.WriteLine(line);
	}

	/// <summary>
	/// Write a small set of items to a memory string.
	/// </summary>
	/// <typeparam name="T"> The type of the values. </typeparam>
	/// <param name="filePath"> The file path to write to. </param>
	/// <param name="settings"> The options for writing. </param>
	/// <param name="values"> The values to write. </param>
	public static void WriteToFile<T>(string filePath, CsvConverterSettings settings, params T[] values)
	{
		using var writer = File.CreateText(filePath);
		Write(writer, settings, values);
	}

	private static string CreateLine(CsvConverterSettings settings, IEnumerable<object> values)
	{
		return string.Join(settings.Delimiter.ToString(), values.Select(x => EscapeValue(x, settings)));
	}

	private static object[] GetObjectValues<T>(T value, PropertyInfo[] headerValues)
	{
		return headerValues
			.Select(x => x.GetValue(value))
			.ToArray();
	}

	private static PropertyInfo[] GetTypeHeaders(Type type)
	{
		return type.GetCachedProperties()
			.OrderBy(x => x.Name)
			.ToArray();
	}

	#endregion
}