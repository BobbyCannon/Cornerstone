using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Remote.Protocol.Viewport;
using Metsys.Bson;

namespace Cornerstone.Presentation.Remote.Protocol
{
    [RequiresUnreferencedCode("Bson uses reflection")]
    class BsonStreamTransportConnection : ICornerstoneRemoteTransportConnection
    {
        private readonly IMessageTypeResolver _resolver;
        private readonly Stream _inputStream;
        private readonly Stream _outputStream;
        private readonly Action _disposeCallback;
        private readonly CancellationToken _cancel;
        private readonly CancellationTokenSource _cancelSource;
        private readonly MemoryStream _outputBlock = new MemoryStream();
        private readonly object _lock = new object();
        private readonly Queue<object> _sendQueue;
        private readonly byte[] _header;
        private byte[] _messageBuffer;
        private byte[] _framePixels;
        private bool _writeOperationPending;
        private bool _readingAlreadyStarted;
        private bool _writerIsBroken;   
        private static readonly byte[] ZeroLength = new byte[4];

        public BsonStreamTransportConnection(IMessageTypeResolver resolver, Stream inputStream, Stream outputStream, Action disposeCallback)
        {
            _resolver = resolver;
            _inputStream = inputStream;
            _outputStream = outputStream;
            _disposeCallback = disposeCallback;
            _cancelSource = new CancellationTokenSource();
            _cancel = _cancelSource.Token;
            _sendQueue = new Queue<object>();
            _header = new byte[20];
            _messageBuffer = Array.Empty<byte>();
            _framePixels = Array.Empty<byte>();
        }

        public void Dispose()
        {
            _cancelSource.Cancel();
            _disposeCallback?.Invoke();
        }
        
        public void StartReading()
        {
            lock (_lock)
            {
                if(_readingAlreadyStarted)
                    throw new InvalidOperationException("Reading has already started");
                _readingAlreadyStarted = true;
                Task.Run(Reader, _cancel);
            }
        }

        async Task ReadExact(byte[] buffer, int count)
        {
            int read = 0;
            while (read != count)
            {
                var readNow = await _inputStream.ReadAsync(buffer, read, count - read, _cancel)
                    .ConfigureAwait(false);
                if (readNow == 0)
                    throw new EndOfStreamException();
                read += readNow;
            }
        }

        byte[] TakeFramePixels(int length)
        {
            if (length <= 0)
            {
                return Array.Empty<byte>();
            }

            var buffer = _framePixels;
            if ((buffer == null) || (buffer.Length != length))
            {
                buffer = new byte[length];
                _framePixels = buffer;
            }

            return buffer;
        }

        async Task Reader()
        {
            try
            {
                while (true)
                {
                    await ReadExact(_header, _header.Length).ConfigureAwait(false);
                    var length = BitConverter.ToInt32(_header, 0);
                    if (length < 0)
                        throw new InvalidDataException("Remote message length is negative.");

                    var guidBytes = new byte[16];
                    Buffer.BlockCopy(_header, 4, guidBytes, 0, 16);
                    var guid = new Guid(guidBytes);
                    if (_messageBuffer.Length < length)
                        _messageBuffer = new byte[length];

                    await ReadExact(_messageBuffer, length).ConfigureAwait(false);
                    var messageType = _resolver.GetByGuid(guid);
                    var payload = new MemoryStream(_messageBuffer, 0, length, false);
                    object message;
                    if (messageType == typeof(FrameMessage))
                    {
                        // Pixels are reused on the next frame. OnMessage copies them before it returns.
                        message = Deserializer.Deserialize(new BinaryReader(payload), messageType, TakeFramePixels);
                    }
                    else
                    {
                        message = Deserializer.Deserialize(new BinaryReader(payload), messageType);
                    }

                    OnMessage?.Invoke(this, message);
                }
            }
            catch (Exception e)
            {
                FireException(e);
            }
        }


        public Task Send(object data)
        {
            lock (_lock)
            {
                // Ignore further calls, since there is no point of writing to a broken stream.
                if (_writerIsBroken)
                {
                    return Task.CompletedTask;
                }

                // A frame write and the XAML result write overlap. Queue instead of throwing
                // away the second message, which drops the host's reply.
                _sendQueue.Enqueue(data);
                if (_writeOperationPending)
                {
                    return Task.CompletedTask;
                }

                _writeOperationPending = true;
            }

            return DrainSendsAsync();
        }

        async Task DrainSendsAsync()
        {
            try
            {
                while (true)
                {
                    object data;
                    lock (_lock)
                    {
                        if ((_sendQueue.Count == 0) || _writerIsBroken)
                        {
                            _writeOperationPending = false;
                            return;
                        }

                        data = _sendQueue.Dequeue();
                    }

                    await WriteOneAsync(data).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                lock (_lock)
                {
                    _writerIsBroken = true;
                    _writeOperationPending = false;
                    _sendQueue.Clear();
                }

                FireException(e);
            }
        }

        async Task WriteOneAsync(object data)
        {
            var guid = _resolver.GetGuid(data.GetType()).ToByteArray();
            var capacity = 1024;
            var frame = data as FrameMessage;
            if ((frame != null) && (frame.Data != null) && (frame.Data.Length > 0))
            {
                capacity = frame.Data.Length + 1024;
            }

            if (_outputBlock.Capacity < capacity)
            {
                _outputBlock.Capacity = capacity;
            }

            _outputBlock.SetLength(0);
            _outputBlock.Position = 0;
            _outputBlock.Write(ZeroLength, 0, 4);
            _outputBlock.Write(guid, 0, guid.Length);
            Serializer.Serialize(data, _outputBlock);
            var total = (int)_outputBlock.Length;
            var length = BitConverter.GetBytes(total - 20);
            _outputBlock.Position = 0;
            _outputBlock.Write(length, 0, 4);
            await _outputStream.WriteAsync(_outputBlock.GetBuffer(), 0, total, _cancel).ConfigureAwait(false);
        }

        void FireException(Exception e)
        {
            var cancel = e as OperationCanceledException;
            if (cancel?.CancellationToken == _cancel)
                return;
            OnException?.Invoke(this, e);
        }


        public event Action<ICornerstoneRemoteTransportConnection, object> OnMessage;
        public event Action<ICornerstoneRemoteTransportConnection, Exception> OnException;
        public void Start()
        {
            
        }
    }
}
