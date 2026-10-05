using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Cornerstone.Presentation.Remote.Protocol
{
    public abstract class TcpTransportBase
    {
        private readonly IMessageTypeResolver _resolver;

        public TcpTransportBase(IMessageTypeResolver resolver)
        {
            _resolver = resolver;
        }
        
        protected abstract ICornerstoneRemoteTransportConnection CreateTransport(IMessageTypeResolver resolver,
            Stream stream, Action disposeCallback);

        class DisposableServer : IDisposable
        {
            private readonly TcpListener _l;

            public DisposableServer(TcpListener l)
            {
                _l = l;
            }
            public void Dispose()
            {
                try
                {
                    _l.Stop();
                }
                catch
                {
                    //Ignore
                }
            }
        }
        
        public IDisposable Listen(IPAddress address, int port, Action<ICornerstoneRemoteTransportConnection> cb)
        {
            var server = new TcpListener(address, port);
            async void AcceptNew()
            {
                try
                {
                    var cl = await server.AcceptTcpClientAsync().ConfigureAwait(false);
                    AcceptNew();
                    await Task.Run(async () =>
                    {
                        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                        var client = cl;
                        var t = CreateTransport(_resolver, client.GetStream(), () =>
                        {
                            // Closing the accepted client is what unblocks a stuck WriteAsync on net472.
                            try
                            {
                                ((IDisposable)client).Dispose();
                            }
                            catch
                            {
                                // Already closed.
                            }
                            tcs.TrySetResult(0);
                        });
                        cb(t);
                        await tcs.Task.ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                catch
                {
                    //Ignore and stop
                }
            }
            server.Start();
            AcceptNew();
            return new DisposableServer(server);
        }

        public async Task<ICornerstoneRemoteTransportConnection> Connect(IPAddress address, int port)
        {
            var c = new TcpClient();
            await c.ConnectAsync(address, port).ConfigureAwait(false);
            return CreateTransport(_resolver, c.GetStream(), ((IDisposable)c).Dispose);
        }
    }
}