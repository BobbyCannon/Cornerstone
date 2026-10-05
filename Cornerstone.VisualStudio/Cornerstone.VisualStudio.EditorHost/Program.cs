#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;

#endregion

namespace Cornerstone.VisualStudio.EditorHost;

public static class Program
{
	#region Methods

	[RequiresUnreferencedCode("Bson uses reflection")]
	public static int Main(string[] args)
	{
		var portIndex = Array.IndexOf(args, "--port");
		var server = new EditorServer(new Metadata());
		if ((portIndex >= 0) && ((portIndex + 1) < args.Length) && int.TryParse(args[portIndex + 1], out var requested) && (requested > 0))
		{
			server.Start(requested);
		}
		else
		{
			server.Start();
		}

		Console.WriteLine(server.Port);
		Console.Out.Flush();
		Thread.Sleep(Timeout.Infinite);
		return 0;
	}

	#endregion
}