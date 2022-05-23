using System;
using BeyondTech.Extensions.Logging.Timing.Configuration;
using Microsoft.Extensions.Logging;

namespace BeyondTech.Extensions.Logging.Timing
{
    /// <summary>
    /// Extends <see cref="ILogger"/> with timed operations.
    /// </summary>
    public static class LoggerOperationExtensions
    {
        /// <summary>
        /// Begin a new timed operation. The return value must be disposed to complete the operation.
        /// </summary>
        /// <param name="logger">The logger through which the timing will be recorded.</param>
        /// <param name="messageTemplate">A log message describing the operation, in message template format.</param>
        /// <param name="args">Arguments to the log message. These will be stored and captured only when the
        /// operation completes, so do not pass arguments that are mutated during the operation.</param>
        /// <returns>An <see cref="Operation"/> object.</returns>
        public static IDisposable TimeOperation(this ILogger logger, string messageTemplate, params object[] args)
        {
            return new Operation(logger, messageTemplate, args, CompletionBehavior.Complete, LogLevel.Information, LogLevel.Warning);
        }

        /// <summary>
        /// Begin a new timed operation. The return value must be completed using <see cref="Operation.Complete()"/>,
        /// or disposed to record abandonment.
        /// </summary>
        /// <param name="logger">The logger through which the timing will be recorded.</param>
        /// <param name="messageTemplate">A log message describing the operation, in message template format.</param>
        /// <param name="args">Arguments to the log message. These will be stored and captured only when the
        /// operation completes, so do not pass arguments that are mutated during the operation.</param>
        /// <returns>An <see cref="Operation"/> object.</returns>
        public static Operation BeginOperation(this ILogger logger, string messageTemplate, params object[] args)
        {
            return new Operation(logger, messageTemplate, args, CompletionBehavior.Abandon, LogLevel.Information, LogLevel.Warning);
        }

        /// <summary>
        /// Configure the logging levels used for completion and abandonment events.
        /// </summary>
        /// <param name="logger">The logger through which the timing will be recorded.</param>
        /// <param name="completion">The level of the event to write on operation completion.</param>
        /// <param name="abandonment">The level of the event to write on operation abandonment; if not
        /// specified, the <paramref name="completion"/> level will be used.</param>
        /// <param name="warningThreshold">The threshold which determines whether the timing will be recorded as warning</param>
        /// <returns>An object from which timings with the configured levels can be made.</returns>
        /// <remarks>If neither <paramref name="completion"/> nor <paramref name="abandonment"/> is enabled
        /// on the logger at the time of the call, a no-op result is returned.</remarks>
        public static LeveledOperation OperationAt(this ILogger logger, LogLevel completion, LogLevel? abandonment = null, TimeSpan? warningThreshold = null)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            var appliedAbandonment = abandonment ?? completion;
            if (!logger.IsEnabled(completion) &&
                (appliedAbandonment == completion || !logger.IsEnabled(appliedAbandonment)))
            {
                return LeveledOperation.None;
            }

            return new LeveledOperation(logger, completion, appliedAbandonment, warningThreshold);
        }
    }
}
