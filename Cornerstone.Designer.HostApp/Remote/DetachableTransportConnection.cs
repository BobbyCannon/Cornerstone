using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.Presentation.DesignerSupport.Remote
{
    class DetachableTransportConnection : ICornerstoneRemoteTransportConnection
    {
        private ICornerstoneRemoteTransportConnection _inner;

        public DetachableTransportConnection(ICornerstoneRemoteTransportConnection inner)
        {
            _inner = inner;
            _inner.OnMessage += FireOnMessage;
        }

        public void Dispose()
        {
            if (_inner != null)
                _inner.OnMessage -= FireOnMessage;
            _inner = null;
        }

        public void FireOnMessage(ICornerstoneRemoteTransportConnection transport, object obj) => OnMessage?.Invoke(transport, obj);
        
        public Task Send(object data)
        {
            return _inner?.Send(data);
        }

        public event Action<ICornerstoneRemoteTransportConnection, object> OnMessage;

        public event Action<ICornerstoneRemoteTransportConnection, Exception> OnException
        {
            add {}
            remove {}
        }

        public void Start() => _inner?.Start();
    }
}
