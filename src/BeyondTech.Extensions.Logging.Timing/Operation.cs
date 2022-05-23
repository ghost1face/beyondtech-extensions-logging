using System;
using System.Diagnostics;
using System.Linq;
using BeyondTech.Extensions.Logging.Timing.Configuration;
using Microsoft.Extensions.Logging;

namespace BeyondTech.Extensions.Logging.Timing
{
    /// <summary>
    /// Records operation timings for an instance of ILogger.
    /// </summary>
    public sealed class Operation : IDisposable
    {
        /// <summary>
        /// Property names attached to events by <see cref="Operation"/>s.
        /// </summary>
        enum Properties
        {
            /// <summary>
            /// The timing, in milliseconds.
            /// </summary>
            Elapsed,

            /// <summary>
            /// Completion status, either <em>completed</em> or <em>discarded</em>.
            /// </summary>
            Outcome,

            /// <summary>
            /// A unique identifier added to the log context during the operation
            /// </summary>
            OperationId
        }

        private const string OutcomeCompleted = "completed";
        private const string OutcomeAbandoned = "abandoned";
        private static readonly long StopwatchToTimeSpanTicks = Stopwatch.Frequency / TimeSpan.TicksPerSecond;

        private readonly ILogger _logger;
        private readonly string _messageTemplate;
        private readonly object[] _args;
        private readonly long _start;
        private long? _stop;

        private readonly IDisposable _popContext;
        private CompletionBehavior _completionBehavior;
        private readonly LogLevel _completionLevel;
        private readonly LogLevel _abandonmentLevel;
        private readonly TimeSpan? _warningThreshold;
        private Exception? _exception;

        internal Operation(ILogger logger, string messageTemplate, object[] args,
            CompletionBehavior completionBehavior, LogLevel completionLevel, LogLevel abandonmentLevel,
            TimeSpan? warningThreshold = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageTemplate = messageTemplate ?? throw new ArgumentNullException(nameof(messageTemplate));
            _args = args ?? throw new ArgumentNullException(nameof(args));
            _completionBehavior = completionBehavior;
            _completionLevel = completionLevel;
            _abandonmentLevel = abandonmentLevel;
            _warningThreshold = warningThreshold;
            _popContext = logger.BeginScope("OperationId: {OperationId}", Guid.NewGuid());
            _start = GetTimestamp();
        }

        private static long GetTimestamp()
        {
            return Stopwatch.GetTimestamp() / StopwatchToTimeSpanTicks;
        }

        /// <summary>
        /// Returns the elapsed time of the operation. This will update during the operation, and be frozen once the
        /// operation is completed or canceled.
        /// </summary>
        public TimeSpan Elapsed
        {
            get
            {
                var stop = _stop ?? GetTimestamp();
                var elapsedTicks = stop - _start;

                if (elapsedTicks < 0)
                {
                    // When measuring small time periods the StopWatch.Elapsed*  properties can return negative values.
                    // This is due to bugs in the basic input/output system (BIOS) or the hardware abstraction layer
                    // (HAL) on machines with variable-speed CPUs (e.g. Intel SpeedStep).
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromTicks(elapsedTicks);
            }
        }

        /// <summary>
        /// Complete the timed operation. This will write the event and elapsed time to the log.
        /// </summary>
        public void Complete()
        {
            if (_completionBehavior == CompletionBehavior.Silent)
                return;

            Write(_logger, _completionLevel, OutcomeCompleted);
        }

        /// <summary>
        /// Complete the timed operation with an included result value.
        /// </summary>
        /// <param name="template">The name for the property to attach to the event.</param>
        /// <param name="args">The result value.</param>
        public void Complete(string template, params object[] args)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            if (_completionBehavior == CompletionBehavior.Silent)
                return;

            using (_logger.BeginScope(template, args))
            {
                Write(_logger, _completionLevel, OutcomeCompleted);
            }
        }

        /// <summary>
        /// Abandon the timed operation. This will write the event and elapsed time to the log.
        /// </summary>
        public void Abandon()
        {
            if (_completionBehavior == CompletionBehavior.Silent)
                return;

            Write(_logger, _abandonmentLevel, OutcomeAbandoned);
        }

        /// <summary>
        /// Cancel the timed operation. After calling, no event will be recorded either through
        /// completion or disposal.
        /// </summary>
        public void Cancel()
        {
            _completionBehavior = CompletionBehavior.Silent;
            PopLogContext();
        }

        /// <summary>
        /// Dispose the operation. If not already completed or canceled, an event will be written
        /// with timing information. Operations started with <see cref="LeveledOperation.Time"/> will be completed through
        /// disposal. Operations started with <see cref="LeveledOperation.Begin"/> will be recorded as abandoned.
        /// </summary>
        public void Dispose()
        {
            switch (_completionBehavior)
            {
                case CompletionBehavior.Silent:
                    break;

                case CompletionBehavior.Abandon:
                    Write(_logger, _abandonmentLevel, OutcomeAbandoned);
                    break;

                case CompletionBehavior.Complete:
                    Write(_logger, _completionLevel, OutcomeCompleted);
                    break;

                default:
                    break;
            }

            PopLogContext();
        }

        private void StopTiming()
        {
            _stop ??= GetTimestamp();
        }

        private void PopLogContext()
        {
            _popContext.Dispose();
        }

        private void Write(ILogger target, LogLevel level, string outcome)
        {
            StopTiming();
            _completionBehavior = CompletionBehavior.Silent;

            var elapsed = Elapsed.TotalMilliseconds;

            level = elapsed > _warningThreshold?.TotalMilliseconds && level < LogLevel.Warning
                ? LogLevel.Warning
                : level;

            target.Log(level, _exception, $"{_messageTemplate} {{{nameof(Properties.Outcome)}}} in {{{nameof(Properties.Elapsed)}:0.0}} ms", _args.Concat(new object[] { outcome, elapsed }).ToArray());

            PopLogContext();
        }

        /// <summary>
        /// Enriches resulting log event with the given exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <returns>Same <see cref="Operation"/>.</returns>
        public Operation SetException(Exception exception)
        {
            _exception = exception;
            return this;
        }
    }
}
