#region References

using Microsoft.AspNetCore.Builder;

#endregion

namespace Sample.Server.Website;

public class Program
{
	#region Methods

	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		var app = builder.Build();

		app.MapGet("/", () => "Hello World!");

		app.Run();
	}

	#endregion
}