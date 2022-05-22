using System;

namespace Microsoft.Extensions.Logging.Timing
{
    /// <summary>
    /// Exception-handling related helpers for <see cref="Operation"/>.
    /// </summary>
    public static class OperationExtensions
    {
        /// <summary>
        /// Enriches resulting log event with the given exception and skips exception-handling block.
        /// </summary>
        /// <param name="operation">Operation to enrich with exception.</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <returns><c>false</c></returns>
        /// <seealso cref="Operation.SetException"/>
        /// <example>
        /// <code>
        /// using (var op = Operation.Begin(...)
        /// {
        ///     try
        ///     {
        ///         //Do something
        ///         op.Complete();
        ///     }
        ///     catch (Exception e) when (op.SetExceptionAndRethrow(e))
        ///     {
        ///         //this will never be called
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool SetExceptionAndRethrow(this Operation operation, Exception exception)
        {
            operation.SetException(exception);
            return false;
        }

        /// <summary>
        /// Abandon the timed operation with an included exception.
        /// </summary>
        /// <param name="operation">Operation to enrich and abandon.</param>
        /// <param name="exception">Enricher related to the event.</param>
        /// <seealso cref="Operation.Abandon()"/>
        /// <seealso cref="Operation.SetException(Exception)"/>
        public static void Abandon(this Operation operation, Exception exception)
            => operation.SetException(exception).Abandon();
    }
}
