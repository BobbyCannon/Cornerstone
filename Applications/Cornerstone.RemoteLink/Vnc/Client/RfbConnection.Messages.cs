using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.RemoteLink.Vnc.Client.Protocol;
using Cornerstone.RemoteLink.Vnc.Client.Protocol.Implementation.MessageTypes.Outgoing;
using Cornerstone.RemoteLink.Vnc.Client.Protocol.MessageTypes;

namespace Cornerstone.RemoteLink.Vnc.Client
{
    public partial class RfbConnection
    {
        /// <summary>
        /// Adds the <paramref name="message"/> to the send queue and returns without waiting for it being sent.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TMessageType">The type of the message.</typeparam>
        /// <returns>True, if the message was queued, otherwise false.</returns>
        /// <remarks>Please ensure the outgoing message type is marked as being supported by both sides before sending it. See <see cref="RfbConnection.UsedMessageTypes"/>.</remarks>
        public bool EnqueueMessage<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RfbConnectionContext? connection = _activeConnection;
            if (connection?.MessageSender == null)
                return false;

            connection.MessageSender.EnqueueMessage(message, cancellationToken);
            return true;
        }

        /// <summary>
        /// Adds the <paramref name="message"/> to the send queue and returns a <see cref="Task"/> that completes when the message was sent.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TMessageType">The type of the message.</typeparam>
        /// <remarks>Please ensure the outgoing message type is marked as being supported by both sides before sending it. See <see cref="RfbConnection.UsedMessageTypes"/>.</remarks>
        public Task SendMessageAsync<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RfbConnectionContext? connection = _activeConnection;
            if (connection?.MessageSender == null)
                return Task.CompletedTask;

            return connection.MessageSender.SendMessageAndWaitAsync(message, cancellationToken);
        }

        /// <summary>
        /// Types <paramref name="text"/> on the remote desktop as key down/up events.
        /// </summary>
        public async Task TypeTextAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            RfbConnectionContext connection = _activeConnection;
            if (connection?.MessageSender == null)
            {
                throw new InvalidOperationException("The connection is not ready to send key events.");
            }

            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            foreach (var c in text)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var keySymbol = GetKeySymbol(c);
                if (keySymbol == KeySymbol.Null)
                {
                    continue;
                }

                await SendMessageAsync(new KeyEventMessage(true, keySymbol), cancellationToken).ConfigureAwait(false);
                await SendMessageAsync(new KeyEventMessage(false, keySymbol), cancellationToken).ConfigureAwait(false);
            }
        }

        private static KeySymbol GetKeySymbol(char c)
        {
            if (c == '\n')
            {
                return KeySymbol.Return;
            }

            if (c == '\t')
            {
                return KeySymbol.Tab;
            }

            if ((c >= ' ') && (c <= '~'))
            {
                return KeySymbol.space + (c - ' ');
            }

            if (c <= 0xff)
            {
                return (KeySymbol)c;
            }

            return (KeySymbol)(0x01000000 | c);
        }
    }
}
