#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Data.Bytes;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data.Bytes;

[TestClass]
[SuppressMessage("ReSharper", "RedundantCast")]
public class ByteSizeExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Bits()
	{
		var scenarios = new Dictionary<ByteSize, int>
		{
			{ new ByteSize(1), 8 }
		};

		if (EnableFileUpdates || IsDebugging)
		{
			GenerateAsserts(nameof(ByteSizeExtensions.Bits));
		}

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, ((byte) scenario.Value).Bits());
			AreEqual(scenario.Key, ((byte?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((sbyte) scenario.Value).Bits());
			AreEqual(scenario.Key, ((sbyte?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((short) scenario.Value).Bits());
			AreEqual(scenario.Key, ((short?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((ushort) scenario.Value).Bits());
			AreEqual(scenario.Key, ((ushort?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((int) scenario.Value).Bits());
			AreEqual(scenario.Key, ((int?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((uint) scenario.Value).Bits());
			AreEqual(scenario.Key, ((uint?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((long) scenario.Value).Bits());
			AreEqual(scenario.Key, ((long?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((ulong) scenario.Value).Bits());
			AreEqual(scenario.Key, ((ulong?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((IntPtr) scenario.Value).Bits());
			AreEqual(scenario.Key, ((IntPtr?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((UIntPtr) scenario.Value).Bits());
			AreEqual(scenario.Key, ((UIntPtr?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((Int128) scenario.Value).Bits());
			AreEqual(scenario.Key, ((Int128?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((UInt128) scenario.Value).Bits());
			AreEqual(scenario.Key, ((UInt128?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((decimal) scenario.Value).Bits());
			AreEqual(scenario.Key, ((decimal?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((double) scenario.Value).Bits());
			AreEqual(scenario.Key, ((double?) scenario.Value).Bits());
			AreEqual(scenario.Key, ((float) scenario.Value).Bits());
			AreEqual(scenario.Key, ((float?) scenario.Value).Bits());
		}
	}

	[TestMethod]
	public void Bytes()
	{
		var scenarios = new Dictionary<ByteSize, int>
		{
			{ new ByteSize(2), 2 }
		};

		if (EnableFileUpdates || IsDebugging)
		{
			GenerateAsserts(nameof(ByteSizeExtensions.Bytes));
		}

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, ((byte) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((byte?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((sbyte) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((sbyte?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((short) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((short?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((ushort) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((ushort?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((int) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((int?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((uint) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((uint?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((long) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((long?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((ulong) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((ulong?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((IntPtr) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((IntPtr?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((UIntPtr) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((UIntPtr?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((Int128) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((Int128?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((UInt128) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((UInt128?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((decimal) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((decimal?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((double) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((double?) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((float) scenario.Value).Bytes());
			AreEqual(scenario.Key, ((float?) scenario.Value).Bytes());
		}
	}

	[TestMethod]
	public void GenerateByteSizeCode()
	{
		if (!IsDebugging)
		{
			return;
		}

		var scenarios = new (string name, string template)[]
		{
			("Bits", "public static ByteSize FromBits($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value / BitsInByte);\r\n}\r\n"),
			("Bytes", "public static ByteSize FromBytes($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value);\r\n}\r\n"),
			("FromKilobits", "public static ByteSize FromKilobits($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value * BytesInKilobit);\r\n}\r\n"),
			("FromKilobytes", "public static ByteSize FromKilobytes($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value * BytesInKilobyte);\r\n}\r\n"),
			("Megabits", "public static ByteSize FromMegabits($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value * BytesInMegabit);\r\n}\r\n"),
			("Megabytes", "public static ByteSize FromMegabytes($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value * BytesInMegabyte);\r\n}\r\n"),
			("Gigabits", "public static ByteSize FromGigabits($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value * BytesInGigabit);\r\n}\r\n"),
			("Gigabytes", "public static ByteSize FromGigabytes($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value * BytesInGigabyte);\r\n}\r\n"),
			("Terabits", "public static ByteSize FromTerabits($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value * BytesInTerabit);\r\n}\r\n"),
			("Terabytes", "public static ByteSize FromTerabytes($TYPE$ value)\r\n{\r\n\treturn new ByteSize((decimal?) value * BytesInTerabyte);\r\n}\r\n")
		};

		var builder = new TextBuilder();
		var builderIsNet7OrGreater = new TextBuilder();

		foreach (var scenario in scenarios)
		{
			foreach (var type in Activator.NumberTypes)
			{
				var codeMethod = scenario.template
					.Replace("$NAME$", scenario.name)
					.Replace("$TYPE$", CSharpCodeWriter.GetCodeTypeName(type));

				if (!type.IsNullableType())
				{
					codeMethod = codeMethod.Replace("(decimal?)", "(decimal)");
				}

				if (Activator.IsNet7OrGreater(type))
				{
					builderIsNet7OrGreater.AppendLine(codeMethod);
				}
				else
				{
					builder.AppendLine(codeMethod);
				}

				builder.AppendLine();
			}
		}

		var path = $"{SolutionDirectory}\\Cornerstone\\Data\\Bytes\\ByteSize.Generated.cs";

		FileModifier.UpdateFileIfNecessary("// <StartGenerated>\r\n", "// </StartGenerated>", path, builder.ToString(), "\t");
		FileModifier.UpdateFileIfNecessary("// <StartGenerated-7+>\r\n", "// </StartGenerated-7+>", path, builderIsNet7OrGreater.ToString(), "\t");
	}

	[TestMethod]
	public void GenerateExtensions()
	{
		if (!IsDebugging)
		{
			return;
		}

		var scenarios = new[]
		{
			"Bits", "Bytes",
			"Kilobits", "Kilobytes",
			"Megabits", "Megabytes",
			"Gigabits", "Gigabytes",
			"Terabits", "Terabytes"
		};
		var builder = new TextBuilder();
		var builderIsNet7OrGreater = new TextBuilder();

		foreach (var scenario in scenarios)
		{
			foreach (var type in Activator.NumberTypes)
			{
				var template = "public static ByteSize $NAME$(this $TYPE$ input)\r\n{\r\n\treturn ByteSize.From$NAME$(input);\r\n}\r\n";
				var codeMethod = template
					.Replace("$NAME$", scenario)
					.Replace("$TYPE$", CSharpCodeWriter.GetCodeTypeName(type));

				if (Activator.IsNet7OrGreater(type))
				{
					builderIsNet7OrGreater.AppendLine(codeMethod);
				}
				else
				{
					builder.AppendLine(codeMethod);
				}

				builder.AppendLine();
			}
		}

		var path = $"{SolutionDirectory}\\Cornerstone\\Data\\Bytes\\ByteSizeExtensions.Generated.cs";

		FileModifier.UpdateFileIfNecessary("// <StartGenerated>\r\n", "// </StartGenerated>", path, builder.ToString(), "\t");
		FileModifier.UpdateFileIfNecessary("// <StartGenerated-7+>\r\n", "// </StartGenerated-7+>", path, builderIsNet7OrGreater.ToString(), "\t");
	}

	[TestMethod]
	public void Gigabits()
	{
		var scenarios = new Dictionary<ByteSize, int>
		{
			{ new ByteSize(125000000), 1 }
		};

		if (EnableFileUpdates || IsDebugging)
		{
			GenerateAsserts(nameof(ByteSizeExtensions.Gigabits));
		}

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, ((byte) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((byte?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((sbyte) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((sbyte?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((short) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((short?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((ushort) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((ushort?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((int) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((int?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((uint) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((uint?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((long) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((long?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((ulong) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((ulong?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((IntPtr) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((IntPtr?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((UIntPtr) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((UIntPtr?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((Int128) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((Int128?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((UInt128) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((UInt128?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((decimal) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((decimal?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((double) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((double?) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((float) scenario.Value).Gigabits());
			AreEqual(scenario.Key, ((float?) scenario.Value).Gigabits());
		}
	}

	[TestMethod]
	public void Gigabytes()
	{
		var scenarios = new Dictionary<ByteSize, int>
		{
			{ new ByteSize(1073741824), 1 }
		};

		if (EnableFileUpdates || IsDebugging)
		{
			GenerateAsserts(nameof(ByteSizeExtensions.Gigabytes));
		}

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, ((byte) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((byte?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((sbyte) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((sbyte?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((short) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((short?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((ushort) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((ushort?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((int) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((int?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((uint) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((uint?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((long) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((long?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((ulong) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((ulong?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((IntPtr) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((IntPtr?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((UIntPtr) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((UIntPtr?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((Int128) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((Int128?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((UInt128) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((UInt128?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((decimal) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((decimal?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((double) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((double?) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((float) scenario.Value).Gigabytes());
			AreEqual(scenario.Key, ((float?) scenario.Value).Gigabytes());
		}
	}

	[TestMethod]
	public void Kilobits()
	{
		var scenarios = new Dictionary<ByteSize, int>
		{
			{ new ByteSize(125), 1 }
		};

		if (EnableFileUpdates || IsDebugging)
		{
			GenerateAsserts(nameof(ByteSizeExtensions.Kilobits));
		}

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, ((byte) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((byte?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((sbyte) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((sbyte?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((short) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((short?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((ushort) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((ushort?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((int) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((int?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((uint) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((uint?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((long) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((long?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((ulong) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((ulong?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((IntPtr) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((IntPtr?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((UIntPtr) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((UIntPtr?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((Int128) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((Int128?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((UInt128) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((UInt128?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((decimal) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((decimal?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((double) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((double?) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((float) scenario.Value).Kilobits());
			AreEqual(scenario.Key, ((float?) scenario.Value).Kilobits());
		}
	}

	[TestMethod]
	public void Kilobytes()
	{
		var scenarios = new Dictionary<ByteSize, int>
		{
			{ new ByteSize(1024), 1 }
		};

		if (EnableFileUpdates || IsDebugging)
		{
			GenerateAsserts(nameof(ByteSizeExtensions.Kilobytes));
		}

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, ((byte) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((byte?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((sbyte) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((sbyte?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((short) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((short?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((ushort) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((ushort?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((int) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((int?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((uint) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((uint?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((long) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((long?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((ulong) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((ulong?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((IntPtr) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((IntPtr?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((UIntPtr) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((UIntPtr?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((Int128) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((Int128?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((UInt128) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((UInt128?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((decimal) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((decimal?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((double) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((double?) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((float) scenario.Value).Kilobytes());
			AreEqual(scenario.Key, ((float?) scenario.Value).Kilobytes());
		}
	}

	[TestMethod]
	public void Megabits()
	{
		var scenarios = new Dictionary<ByteSize, int>
		{
			{ new ByteSize(125000), 1 }
		};

		if (EnableFileUpdates || IsDebugging)
		{
			GenerateAsserts(nameof(ByteSizeExtensions.Megabits));
		}

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, ((byte) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((byte?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((sbyte) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((sbyte?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((short) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((short?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((ushort) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((ushort?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((int) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((int?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((uint) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((uint?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((long) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((long?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((ulong) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((ulong?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((IntPtr) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((IntPtr?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((UIntPtr) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((UIntPtr?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((Int128) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((Int128?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((UInt128) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((UInt128?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((decimal) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((decimal?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((double) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((double?) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((float) scenario.Value).Megabits());
			AreEqual(scenario.Key, ((float?) scenario.Value).Megabits());
		}
	}

	[TestMethod]
	public void Megabytes()
	{
		var scenarios = new Dictionary<ByteSize, int>
		{
			{ new ByteSize(1048576), 1 }
		};

		if (EnableFileUpdates || IsDebugging)
		{
			GenerateAsserts(nameof(ByteSizeExtensions.Megabytes));
		}

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, ((byte) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((byte?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((sbyte) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((sbyte?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((short) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((short?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((ushort) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((ushort?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((int) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((int?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((uint) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((uint?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((long) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((long?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((ulong) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((ulong?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((IntPtr) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((IntPtr?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((UIntPtr) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((UIntPtr?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((Int128) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((Int128?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((UInt128) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((UInt128?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((decimal) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((decimal?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((double) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((double?) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((float) scenario.Value).Megabytes());
			AreEqual(scenario.Key, ((float?) scenario.Value).Megabytes());
		}
	}

	private void GenerateAsserts(string name)
	{
		var builder = new TextBuilder();

		foreach (var type in Activator.NumberTypes)
		{
			var line = $"AreEqual(scenario.Key, (({type.GetCodeTypeName()}) scenario.Value).{name}());";
			builder.AppendLine(line);
		}

		CopyToClipboard(builder);
	}

	#endregion
}