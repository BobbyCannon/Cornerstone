#region References

using System.Threading.Tasks;
using Cornerstone.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Sync;

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
		injector.AddTransient<AccountSync>();
		//injector.AddSingleton<AddressSync>();
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

		var account = injector.GetInstance<AccountSync>();
		AreEqual(values[0].Account, account);
		IsFalse(ReferenceEquals(values[0].Account, account));
	}

	[TestMethod]
	public void AddSingletonMicrosoftHost()
	{
		var builder = Host.CreateApplicationBuilder();

		builder.Services.AddSingleton<AccountSync>();
		//builder.Services.AddSingleton<AddressSync>();
		builder.Services.AddSingleton<Test>();

		using var host = builder.Build();
		host.StartAsync();
		var account = host.Services.GetInstance<Test>();
		Assert.IsNotNull(account);
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

		public Test(AccountSync account)
		{
			Account = account;
		}

		public Test(AddressSync test)
		{
		}

		#endregion

		#region Properties

		public AccountSync Account { get; }

		#endregion
	}

	#endregion
}