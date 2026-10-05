using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platforms.MacOS.Interop;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal sealed class ClipboardImpl(ICsnClipboard native) : IOwnedClipboardImpl, IDisposable
    {
        private ICsnClipboard? _native = native;
        private long _lastClearChangeCount = long.MinValue;

        internal ICsnClipboard Native
            => _native ?? throw new ObjectDisposedException(nameof(ClipboardImpl));

        private void ClearCore()
        {
            _lastClearChangeCount = Native.Clear();
        }

        public Task ClearAsync()
        {
            try
            {
                ClearCore();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public Task<IAsyncDataTransfer?> TryGetDataAsync()
        {
            try
            {
                return Task.FromResult(TryGetData());
            }
            catch (Exception ex)
            {
                return Task.FromException<IAsyncDataTransfer?>(ex);
            }
        }

        private IAsyncDataTransfer? TryGetData()
        {
            var dataTransfer = new ClipboardDataTransfer(
                new ClipboardReadSession(Native, Native.ChangeCount, ownsNative: false));

            if (dataTransfer.Formats.Length == 0)
            {
                dataTransfer.Dispose();
                return null;
            }

            return dataTransfer;
        }

        public Task SetDataAsync(IAsyncDataTransfer dataTransfer)
        {
            try
            {
                SetData(dataTransfer);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        private void SetData(IAsyncDataTransfer dataTransfer)
        {
            ClearCore();

            Native.SetData(new DataTransferToCsnClipboardDataSourceWrapper(
                dataTransfer.ToSynchronous(LogArea.macOSPlatform)));
        }

        public Task<bool> IsCurrentOwnerAsync()
            => Task.FromResult(Native.ChangeCount == _lastClearChangeCount);

        public void Dispose()
        {
            _native?.Dispose();
            _native = null;
        }
    }
}
