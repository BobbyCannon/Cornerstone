#region References

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Remote.Protocol;
using Cornerstone.Presentation.Remote.Protocol.Viewport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Remote;

[TestClass]
public class TransportConnectionWrapperTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	[Timeout(10000)]
	public async Task IdleWakeDoesNotRunInnerSendOnCallerThread()
	{
		var secondBlock = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondEntered = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var sendCount = 0;
		var sendThreadId = 0;
		var inner = new DelegateConnection(async _ =>
		{
			var n = Interlocked.Increment(ref sendCount);
			sendThreadId = Environment.CurrentManagedThreadId;
			if (n == 1)
			{
				return;
			}

			secondEntered.TrySetResult(0);
			await secondBlock.Task;
		});
		var wrapper = new TransportConnectionWrapper(inner);
		await wrapper.Send(new object());
		var callerThread = Environment.CurrentManagedThreadId;
		var second = wrapper.Send(new object());
		Assert.IsFalse(second.IsCompleted);
		await secondEntered.Task;
		Assert.AreNotEqual(callerThread, sendThreadId);
		secondBlock.TrySetResult(0);
		await second;
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ListenDisposeClosesAcceptedClient()
	{
		var transport = new BsonTcpTransport();
		var accepted = new TaskCompletionSource<ICornerstoneRemoteTransportConnection>(
			TaskCreationOptions.RunContinuationsAsynchronously);
		var port = FreeTcpPort();
		using (transport.Listen(IPAddress.Loopback, port, c => accepted.TrySetResult(c)))
		{
			var client = await transport.Connect(IPAddress.Loopback, port);
			var server = await accepted.Task;
			var faulted = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
			client.OnException += (_, ex) => faulted.TrySetResult(ex);
			server.Dispose();
			var completed = await Task.WhenAny(faulted.Task, Task.Delay(5000));
			Assert.AreSame(faulted.Task, completed, "Disposing the listen connection should close the accepted TcpClient.");
			client.Dispose();
		}
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task SendDoesNotBlockCallerWhileInnerSendIsOutstanding()
	{
		var entered = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var inner = new DelegateConnection(async _ =>
		{
			entered.TrySetResult(0);
			await release.Task;
		});
		var wrapper = new TransportConnectionWrapper(inner);
		var callerThread = Environment.CurrentManagedThreadId;
		var sendTask = wrapper.Send(new object());
		Assert.IsFalse(sendTask.IsCompleted);
		await entered.Task;
		Assert.AreNotEqual(callerThread, inner.LastSendThreadId);
		release.TrySetResult(0);
		await sendTask;
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task FrameMessageRoundTripsPixels()
	{
		var transport = new BsonTcpTransport();
		var accepted = new TaskCompletionSource<ICornerstoneRemoteTransportConnection>(
			TaskCreationOptions.RunContinuationsAsynchronously);
		var port = FreeTcpPort();
		using (transport.Listen(IPAddress.Loopback, port, connection => accepted.TrySetResult(connection)))
		{
			var client = await transport.Connect(IPAddress.Loopback, port);
			var server = await accepted.Task;
			var first = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			var second = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			var count = 0;
			long sequenceId = 0;
			var width = 0;
			var height = 0;
			var stride = 0;
			double dpiX = 0;
			double dpiY = 0;
			server.OnException += (_, ex) =>
			{
				first.TrySetException(ex);
				second.TrySetException(ex);
			};
			client.OnException += (_, ex) =>
			{
				first.TrySetException(ex);
				second.TrySetException(ex);
			};
			server.OnMessage += (_, message) =>
			{
				var frame = message as FrameMessage;
				if ((frame == null) || (frame.Data == null))
				{
					return;
				}

				// The transport reuses the pixel buffer on the next frame. Copy before returning.
				var copy = new byte[frame.Data.Length];
				Buffer.BlockCopy(frame.Data, 0, copy, 0, copy.Length);
				var n = Interlocked.Increment(ref count);
				if (n == 1)
				{
					sequenceId = frame.SequenceId;
					width = frame.Width;
					height = frame.Height;
					stride = frame.Stride;
					dpiX = frame.DpiX;
					dpiY = frame.DpiY;
					first.TrySetResult(copy);
				}
				else
				{
					second.TrySetResult(copy);
				}
			};

			await client.Send(new ClientRenderInfoMessage { DpiX = 120, DpiY = 120 });

			var payload = new byte[64 * 1024];
			for (var i = 0; i < payload.Length; i++)
			{
				payload[i] = (byte)(i * 31);
			}

			await client.Send(new FrameMessage
			{
				SequenceId = 7,
				Format = PixelFormat.Bgra8888,
				Data = payload,
				Width = 128,
				Height = 128,
				Stride = 512,
				DpiX = 96,
				DpiY = 192
			});

			var got = await first.Task;
			Assert.AreEqual(7, sequenceId);
			Assert.AreEqual(128, width);
			Assert.AreEqual(128, height);
			Assert.AreEqual(512, stride);
			Assert.AreEqual(96, dpiX);
			Assert.AreEqual(192, dpiY);
			CollectionAssert.AreEqual(payload, got);

			payload[0] = 9;
			payload[1000] = 4;
			payload[payload.Length - 1] = 8;
			await client.Send(new FrameMessage
			{
				SequenceId = 8,
				Format = PixelFormat.Bgra8888,
				Data = payload,
				Width = 128,
				Height = 128,
				Stride = 512,
				DpiX = 96,
				DpiY = 192
			});

			var gotSecond = await second.Task;
			CollectionAssert.AreEqual(payload, gotSecond);
			client.Dispose();
			server.Dispose();
		}
	}

	private static int FreeTcpPort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint) listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	#endregion

	#region Classes

	private sealed class DelegateConnection : ICornerstoneRemoteTransportConnection
	{
		#region Fields

		private readonly Func<object, Task> _send;

		#endregion

		#region Constructors

		public DelegateConnection(Func<object, Task> send)
		{
			_send = send;
			LastSendThreadId = 0;
		}

		#endregion

		#region Properties

		public int LastSendThreadId { get; private set; }

		#endregion

		#region Methods

		public void Dispose()
		{
		}

		public Task Send(object data)
		{
			LastSendThreadId = Environment.CurrentManagedThreadId;
			return _send(data);
		}

		public void Start()
		{
		}

		#endregion

		#region Events

		public event Action<ICornerstoneRemoteTransportConnection, Exception> OnException
		{
			add { }
			remove { }
		}

		public event Action<ICornerstoneRemoteTransportConnection, object> OnMessage
		{
			add { }
			remove { }
		}

		#endregion
	}

	#endregion
}