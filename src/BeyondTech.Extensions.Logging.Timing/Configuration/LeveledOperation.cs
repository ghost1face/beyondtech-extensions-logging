using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BeyondTech.Extensions.Logging.Timing.Configuration
{
    /// <summary>
    /// Launches <see cref="Operation"/>s with non-default completion and abandonment levels.
    /// </summary>
    public class LeveledOperation
    {
        private readonly Operation? _cachedResult;

        private readonly ILogger? _logger;
        private readonly LogLevel _completion;
        private readonly LogLevel _abandonment;
        private readonly TimeSpan? _warningThreshold;

        internal LeveledOperation(ILogger logger, LogLevel completion, LogLevel abandonment, TimeSpan? warningThreshold = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _completion = completion;
            _abandonment = abandonment;
            _warningThreshold = warningThreshold;
        }

        LeveledOperation(Operation cachedResult)
        {
            _cachedResult = cachedResult ?? throw new ArgumentNullException(nameof(cachedResult));
        }

        internal static LeveledOperation None { get; } = new LeveledOperation(
            new Operation(
                NullLogger.Instance,
                "", Array.Empty<object>(),
                CompletionBehavior.Silent,
                LogLevel.Critical,
                LogLevel.Critical));

        /// <summary>
        /// Begin a new timed operation. The return value must be completed using <see cref="Operation.Complete()"/>,
        /// or disposed to record abandonment.
        /// </summary>
        /// <param name="messageTemplate">A log message describing the operation, in message template format.</param>
        /// <param name="args">Arguments to the log message. These will be stored and captured only when the
        /// operation completes, so do not pass arguments that are mutated during the operation.</param>
        /// <returns>An <see cref="Operation"/> object.</returns>
        public Operation Begin(string messageTemplate, params object[] args)
        {
            return _cachedResult ?? new Operation(_logger!, messageTemplate, args, CompletionBehavior.Abandon, _completion, _abandonment, _warningThreshold);
        }

        /// <summary>
        /// Begin a new timed operation. The return value must be disposed to complete the operation.
        /// </summary>
        /// <param name="messageTemplate">A log message describing the operation, in message template format.</param>
        /// <param name="args">Arguments to the log message. These will be stored and captured only when the
        /// operation completes, so do not pass arguments that are mutated during the operation.</param>
        /// <returns>An <see cref="Operation"/> object.</returns>
        public IDisposable Time(string messageTemplate, params object[] args)
        {
            return _cachedResult ?? new Operation(_logger!, messageTemplate, args, CompletionBehavior.Complete, _completion, _abandonment, _warningThreshold);
        }
    }
}
