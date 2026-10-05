using System;
using System.Threading.Tasks;

namespace Cornerstone.Presentation.Remote.Protocol
{
    public interface ICornerstoneRemoteTransportConnection : IDisposable
    {
        Task Send(object data);
        event Action<ICornerstoneRemoteTransportConnection, object> OnMessage;
        event Action<ICornerstoneRemoteTransportConnection, Exception> OnException;
        void Start();
    }
}
