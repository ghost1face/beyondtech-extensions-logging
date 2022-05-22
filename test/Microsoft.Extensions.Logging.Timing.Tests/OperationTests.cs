using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging.Timing.Tests.Utilities;
using Xunit;

namespace Microsoft.Extensions.Logging.Timing.Tests
{
    public class OperationTests
    {
        private const string completedText = "completed";
        private const string abandonedText = "abandoned";

        [Fact]
        public void TimeOperation_LogsCompleted_WhenDisposed()
        {
            var logs = new List<string>();
            const string operationName = "Starting operation";

            using (var logger = new TailLogger(logs))
            using (logger.Logger.TimeOperation(operationName))
            {
                // no op
            }

            Assert.Single(logs);
            Assert.Contains($"{operationName} {completedText}", logs[0]);
        }

        [Fact]
        public void TimeOperation_LogsElapsedTime()
        {
            var logs = new List<string>();
            const string operationName = "Starting operation";

            using (var logger = new TailLogger(logs))
            using (logger.Logger.TimeOperation(operationName))
            {
                // no op
            }

            Assert.Matches(@"\d+\.?\d* ms", logs[0]);
        }

        [Fact]
        public void TimeOperation_LogsCompletedWhenExceptionThrown()
        {
            var logs = new List<string>();
            try
            {
                const string operationName = "Starting operation";

                using (var logger = new TailLogger(logs))
                using (logger.Logger.TimeOperation(operationName))
                {
                    throw new Exception("Uh oh");
                }
            }
            catch { }

            Assert.Single(logs);
            Assert.Contains(completedText, logs[0]);
        }

        [Fact]
        public void BeginOperation_LogsAbandonedWhenExceptionThrown()
        {
            var logs = new List<string>();
            try
            {
                const string operationName = "Begin Op!";

                using (var logger = new TailLogger(logs))
                using (logger.Logger.BeginOperation(operationName))
                {
                    throw new Exception("Uh oh");
                }
            }
            catch { }

            Assert.Single(logs);
            Assert.Contains(abandonedText, logs[0]);
        }

        [Fact]
        public void BeginOperation_LogsCompleteWhenExplicitlyCalled()
        {
            var logs = new List<string>();

            const string operationName = "Begin Op!";

            using (var logger = new TailLogger(logs))
            using (var operation = logger.Logger.BeginOperation(operationName))
            {
                operation.Complete();
            }

            Assert.Single(logs);
            Assert.Contains(completedText, logs[0]);
        }

        [Fact]
        public void BeginOperation_LogsAbandonedWhenExplicitlyCalled()
        {
            var logs = new List<string>();

            const string operationName = "Begin Op!";

            using (var logger = new TailLogger(logs))
            using (var operation = logger.Logger.BeginOperation(operationName))
            {
                operation.Abandon();
            }

            Assert.Single(logs);
            Assert.Contains(abandonedText, logs[0]);
        }

        [Fact]
        public void BeginOperation_LogsNothingWhenCanceled()
        {
            var logs = new List<string>();

            const string operationName = "Begin Op!";

            using (var logger = new TailLogger(logs))
            using (var operation = logger.Logger.BeginOperation(operationName))
            {
                operation.Cancel();
            }

            Assert.Empty(logs);
        }

        [Fact]
        public void OperationAt_LogsWarningWhenTooLong()
        {
            var logs = new List<string>();

            const string operationName = "Long Op!";

            using (var logger = new TailLogger(logs))
            using (var operation = logger.Logger.OperationAt(LogLevel.Debug, LogLevel.Critical, TimeSpan.FromMilliseconds(1)).Time(operationName))
            {
                Thread.Sleep(20);
            }

            Assert.Single(logs);
            Assert.StartsWith("warn:", logs[0]);
        }

        [Fact]
        public void OperationAt_LogsSpecifiedLevelWhenAbandoned()
        {
            var logs = new List<string>();

            const string operationName = "Long Op!";

            using (var logger = new TailLogger(logs))
            using (var operation = logger.Logger.OperationAt(LogLevel.Debug, LogLevel.Critical, TimeSpan.FromMilliseconds(1)).Begin(operationName))
            {
                operation.Abandon();
            }

            Assert.Single(logs);
            Assert.StartsWith("crit:", logs[0]);
        }

        [Fact]
        public void OperationAt_LogsSpecifiedLevelWhenComplete()
        {
            var logs = new List<string>();

            const string operationName = "Long Op!";

            using (var logger = new TailLogger(logs))
            using (var operation = logger.Logger.OperationAt(LogLevel.Debug, LogLevel.Critical, TimeSpan.FromMilliseconds(1)).Begin(operationName))
            {
                operation.Complete();
            }

            Assert.Single(logs);
            Assert.StartsWith("dbug:", logs[0]);
        }

        [Fact]
        public void Operation_LogsExceptionWhenSet()
        {
            var logs = new List<string>();

            const string operationName = "Long Op!";

            using (var logger = new TailLogger(logs))
            using (var operation = logger.Logger.BeginOperation(operationName))
            {
                operation.Abandon(new Exception("blah"));
            }

            Assert.Single(logs);
            Assert.Contains("System.Exception", logs[0]);
        }
    }

}