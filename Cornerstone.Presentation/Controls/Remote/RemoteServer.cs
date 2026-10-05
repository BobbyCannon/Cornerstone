using System;
using Cornerstone.Presentation.Controls.Embedding;
using Cornerstone.Presentation.Controls.Remote.Server;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.Presentation.Controls.Remote
{
    internal class RemoteServer : IDisposable
    {
        private EmbeddableControlRoot _topLevel;

        class EmbeddableRemoteServerTopLevelImpl : RemoteServerTopLevelImpl
        {
            public EmbeddableRemoteServerTopLevelImpl(ICornerstoneRemoteTransportConnection transport) : base(transport)
            {
            }
        }
        
        public RemoteServer(ICornerstoneRemoteTransportConnection transport)
        {
            _topLevel = new EmbeddableControlRoot(new EmbeddableRemoteServerTopLevelImpl(transport));
            _topLevel.Prepare();
            _topLevel.StartRendering();
            //TODO: Somehow react on closed connection?
        }

        public object? Content
        {
            get => _topLevel.Content;
            set => _topLevel.Content = value;
        }

        public void Dispose()
        {
            _topLevel.StopRendering();
            _topLevel.Dispose();
        }
    }
}
