#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class WebSyncClientTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void EmptyConverterSkipsOutgoingChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");
			var inner = new SampleServerSyncClient("Server", provider, this, new SyncStatistics(), new Profiler("Server"));
			var client = new WebSyncClient("Web", this, provider, new SampleWebClient(inner));
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>();
			client.BeginSync(sessionId, settings);
			IsNotNull(client.Converter);
			IsFalse(client.Converter.CanConvertOutgoing(typeof(AddressEntity).ToAssemblyName()));
			var changes = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = DateTime.MaxValue.Subtract(TimeSpan.FromDays(1)),
				Take = 100
			});
			AreEqual(0, changes.Collection.Count);
		});
	}

	[TestMethod]
	public void ProviderReturnsWebSyncClient()
	{
		WithEachProvider((provider, _) =>
		{
			var inner = new SampleServerSyncClient("Server", provider, this, new SyncStatistics(), new Profiler("Server"));
			var webClient = new SampleWebClient(inner);
			var syncProvider = new WebServerSyncClientProvider(this, provider, webClient);
			var client = syncProvider.GetSyncClient(new SyncStatistics(), new Profiler("Web"));
			var server = syncProvider.GetServerSyncClient(new SyncStatistics(), new Profiler("WebServer"));
			IsTrue(client is WebSyncClient);
			IsTrue(server is WebSyncClient);
			IsNotNull(syncProvider.GetSyncableDatabase());
		});
	}

	[TestMethod]
	public void SyncPostsToConfiguredUri()
	{
		WithEachProvider((provider, _) =>
		{
			var inner = new SampleServerSyncClient("Server", provider, this, new SyncStatistics(), new Profiler("Server"));
			var capture = new CaptureUriWebClient(inner);
			var client = new WebSyncClient("Web", this, provider, capture, "api/CustomSync");
			var result = client.Sync(new SyncOperation
			{
				SessionId = Guid.NewGuid(),
				Settings = new SyncSettings { IncludeIssueDetails = true },
				EndSession = true
			});
			AreEqual("api/CustomSync", capture.LastUri);
			IsTrue(result.SessionEnded);
		});
	}

	#endregion

	#region Classes

	private sealed class CaptureUriWebClient : SampleWebClient
	{
		#region Constructors

		public CaptureUriWebClient(SampleServerSyncClient serverClient) : base(serverClient)
		{
			LastUri = string.Empty;
		}

		#endregion

		#region Properties

		public string LastUri { get; private set; }

		#endregion

		#region Methods

		public override TResult Post<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
		{
			LastUri = uri;
			return base.Post<TContent, TResult>(uri, content, timeout);
		}

		#endregion
	}

	#endregion
}