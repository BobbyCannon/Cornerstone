using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cornerstone.RemoteLink.Vnc.Controls.Adapters.Logging
{
    /// <summary>
    /// Provider for the <see cref="PresentationLogger"/> logging adapter.
    /// </summary>
    public class PresentationLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, PresentationLogger> _loggers =
            new ConcurrentDictionary<string, PresentationLogger>();

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            if (categoryName == null)
                throw new ArgumentNullException(nameof(categoryName));

            return _loggers.GetOrAdd(categoryName, loggerName => new PresentationLogger(categoryName));
        }

        /// <inheritdoc />
        public void Dispose() { }
    }
}
