using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Extensions.Logging.Timing
{
    /// <summary>
    /// Records operation timings for an instance of ILogger.
    /// </summary>
    public sealed class Operation : IDisposable
    {
        /// <summary>
        /// Property names attached to events by <see cref="Operation"/>s.
        /// </summary>
        public enum Properties
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
        Exception? _exception;

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

        ///// <summary>
        ///// Begin a new timed operation. The return value must be completed using <see cref="Complete()"/>,
        ///// or disposed to record abandonment.
        ///// </summary>
        ///// <param name="messageTemplate">A log message describing the operation, in message template format.</param>
        ///// <param name="args">Arguments to the log message. These will be stored and captured only when the
        ///// operation completes, so do not pass arguments that are mutated during the operation.</param>
        ///// <returns>An <see cref="Operation"/> object.</returns>
        //public static Operation Begin(string messageTemplate, params object[] args)
        //{
        //    return Log.Logger.BeginOperation(messageTemplate, args);
        //}

        ///// <summary>
        ///// Begin a new timed operation. The return value must be disposed to complete the operation.
        ///// </summary>
        ///// <param name="messageTemplate">A log message describing the operation, in message template format.</param>
        ///// <param name="args">Arguments to the log message. These will be stored and captured only when the
        ///// operation completes, so do not pass arguments that are mutated during the operation.</param>
        ///// <returns>An <see cref="Operation"/> object.</returns>
        //public static IDisposable Time(string messageTemplate, params object[] args)
        //{
        //    return Log.Logger.TimeOperation(messageTemplate, args);
        //}

        ///// <summary>
        ///// Configure the logging levels used for completion and abandonment events.
        ///// </summary>
        ///// <param name="completion">The level of the event to write on operation completion.</param>
        ///// <param name="abandonment">The level of the event to write on operation abandonment; if not
        ///// specified, the <paramref name="completion"/> level will be used.</param>
        ///// <returns>An object from which timings with the configured levels can be made.</returns>
        ///// <remarks>If neither <paramref name="completion"/> nor <paramref name="abandonment"/> is enabled
        ///// on the logger at the time of the call, a no-op result is returned.</remarks>
        //public static LevelledOperation At(LogLevel completion, LogLevel? abandonment = null)
        //{
        //    return Log.Logger.OperationAt(completion, abandonment);
        //}

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
        /// <param name="resultPropertyName">The name for the property to attach to the event.</param>
        /// <param name="result">The result value.</param>
        public void Complete(string resultPropertyName, object result)
        {
            if (resultPropertyName == null) throw new ArgumentNullException(nameof(resultPropertyName));

            if (_completionBehavior == CompletionBehavior.Silent)
                return;

            using (_logger.BeginScope($"{resultPropertyName}: {{Result}}", result))
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
        /// with timing information. Operations started with <see cref="Time"/> will be completed through
        /// disposal. Operations started with <see cref="Begin"/> will be recorded as abandoned.
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
                    throw new InvalidOperationException("Unknown underlying state value");
            }

            PopLogContext();
        }

        void StopTiming()
        {
            _stop ??= GetTimestamp();
        }

        void PopLogContext()
        {
            _popContext.Dispose();
        }

        void Write(ILogger target, LogLevel level, string outcome)
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

        ///// <summary>
        ///// Enriches resulting log event via the provided enricher.
        ///// </summary>
        ///// <param name="enricher">Enricher that applies in the context.</param>
        ///// <returns>Same <see cref="Operation"/>.</returns>
        ///// <seealso cref="ILogger.ForContext(ILogEventEnricher)"/>
        //public Operation EnrichWith(ILogEventEnricher enricher)
        //{
        //    _logger = _logger.ForContext(enricher);
        //    return this;
        //}

        ///// <summary>
        ///// Enriches resulting log event via the provided enrichers.
        ///// </summary>
        ///// <param name="enrichers">Enrichers that apply in the context.</param>
        ///// <returns>A logger that will enrich log events as specified.</returns>
        ///// <returns>Same <see cref="Operation"/>.</returns>
        ///// <seealso cref="ILogger.ForContext(IEnumerable{ILogEventEnricher})"/>
        //public Operation EnrichWith(IEnumerable<ILogEventEnricher> enrichers)
        //{
        //    _logger = _logger.ForContext(enrichers);
        //    return this;
        //}

        ///// <summary>
        ///// Enriches resulting log event with the specified property.
        ///// </summary>
        ///// <param name="propertyName">The name of the property. Must be non-empty.</param>
        ///// <param name="value">The property value.</param>
        ///// <param name="destructureObjects">If true, the value will be serialized as a structured
        ///// object if possible; if false, the object will be recorded as a scalar or simple array.</param>
        ///// <returns>Same <see cref="Operation"/>.</returns>
        ///// <seealso cref="ILogger.ForContext(string,object,bool)"/>
        //public Operation EnrichWith(string propertyName, object value, bool destructureObjects = false)
        //{
        //    _logger = _logger.ForContext(propertyName, value, destructureObjects);
        //    return this;
        //}

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
