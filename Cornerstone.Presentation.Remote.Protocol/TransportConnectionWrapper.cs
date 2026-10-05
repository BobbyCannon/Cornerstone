using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cornerstone.Presentation.Remote.Protocol
{
    public class TransportConnectionWrapper : ICornerstoneRemoteTransportConnection
    {
        private readonly ICornerstoneRemoteTransportConnection _conn;
        private EventStash<object> _onMessage;
        private EventStash<Exception> _onException;
        
        private Queue<SendOperation> _sendQueue = new Queue<SendOperation>();
        private object _lock =new object();
        private TaskCompletionSource<int> _signal;
        private bool _workerIsAlive;
        public TransportConnectionWrapper(ICornerstoneRemoteTransportConnection conn)
        {
            _conn = conn;
            _onException = new EventStash<Exception>(this);
            _onMessage = new EventStash<object>(this, e => _onException.Fire(this, e));
            _conn.OnException +=_onException.Fire;
            conn.OnMessage +=  _onMessage.Fire;

        }

        class SendOperation
        {
            public object Message { get; set; }
            public TaskCompletionSource<int> Tcs { get; set; }
        }
        
        public void Dispose() => _conn.Dispose();

        async Task Worker()
        {
            while (true)
            {
                SendOperation wi = null;
                TaskCompletionSource<int> idle = null;
                lock (_lock)
                {
                    if (_sendQueue.Count != 0)
                    {
                        wi = _sendQueue.Dequeue();
                    }
                    else
                    {
                        idle = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                        _signal = idle;
                    }
                }
                if (wi == null)
                {
                    await idle.Task.ConfigureAwait(false);
                    continue;
                }
                try
                {
                    await _conn.Send(wi.Message).ConfigureAwait(false);
                    wi.Tcs.TrySetResult(0);
                }
                catch (Exception e)
                {
                    wi.Tcs.TrySetException(e);
                }
            }    
        }
        
        public Task Send(object data)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<int> signalToComplete = null;
            var startWorker = false;
            lock (_lock)
            {
                if (!_workerIsAlive)
                {
                    _workerIsAlive = true;
                    startWorker = true;
                }
                _sendQueue.Enqueue(new SendOperation
                {
                    Message = data,
                    Tcs = tcs
                });
                if (_signal != null)
                {
                    signalToComplete = _signal;
                    _signal = null;
                }
            }
            // Worker and idle wake must not run CopyToAsync on the caller (often the VS UI thread).
            if (startWorker)
            {
                _ = Task.Run((Func<Task>)Worker);
            }
            signalToComplete?.TrySetResult(0);
            return tcs.Task;
        }
        
        public event Action<ICornerstoneRemoteTransportConnection, object> OnMessage
        {
            add => _onMessage.Add(value);
            remove => _onMessage.Remove(value);
        }

        public event Action<ICornerstoneRemoteTransportConnection, Exception> OnException
        {
            add => _onException.Add(value);
            remove => _onException.Remove(value);
        }

        public void Start() => _conn.Start();
    }
}
