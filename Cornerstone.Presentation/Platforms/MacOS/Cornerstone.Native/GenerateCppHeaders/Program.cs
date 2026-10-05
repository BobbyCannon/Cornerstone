using MicroCom.CodeGenerator;

var idlPath = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "csn.idl"));
var outPath = args.Length > 1 ? args[1] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "inc", "cornerstone-native.h"));

var parsed = MicroComCodeGenerator.Parse(File.ReadAllText(idlPath));
Directory.CreateDirectory(Path.GetDirectoryName(outPath));
File.WriteAllText(outPath, parsed.GenerateCppHeader());
Console.WriteLine(outPath);
