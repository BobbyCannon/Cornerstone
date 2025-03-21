#region References

using System.Threading.Tasks;
using Cornerstone.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Sync;

#endregion

namespace Cornerstone.UnitTests;

[TestClass]
public class InstantiatorTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddSingleton()
	{
		var injector = new DependencyProvider(nameof(InstantiatorTests));
		injector.AddTransient<Account>();
		//injector.AddSingleton<Address>();
		injector.AddSingleton<Test>();

		var values = new Test[99];

		Parallel.For(0, values.Length, i =>
		{
			values[i] = injector.GetInstance<Test>();
			IsNotNull(values[i]);
		});

		for (var i = 1; i < values.Length; i++)
		{
			AreEqual(values[0], values[i]);
			IsTrue(ReferenceEquals(values[0], values[i]));
			AreEqual(values[0].Account, values[i].Account);
			IsTrue(ReferenceEquals(values[0].Account, values[i].Account));
		}

		var account = injector.GetInstance<Account>();
		AreEqual(values[0].Account, account);
		IsFalse(ReferenceEquals(values[0].Account, account));
	}

	[TestMethod]
	public void AddSingletonMicrosoftHost()
	{
		var builder = Host.CreateApplicationBuilder();

		builder.Services.AddSingleton<Account>();
		//builder.Services.AddSingleton<Address>();
		builder.Services.AddSingleton<Test>();

		using var host = builder.Build();
		host.StartAsync();
		var account = host.Services.GetInstance<Test>();
		IsNotNull(account);
	}

	#endregion

	#region Classes

	public class Test
	{
		#region Constructors

		public Test(int test)
		{
		}

		public Test(string test)
		{
		}

		public Test(Account account)
		{
			Account = account;
		}

		public Test(Address test)
		{
		}

		#endregion

		#region Properties

		public Account Account { get; }

		#endregion
	}

	#endregion
}