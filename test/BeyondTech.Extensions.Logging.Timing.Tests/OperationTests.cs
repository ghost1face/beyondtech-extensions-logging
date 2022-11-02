using System;
using System.Collections.Generic;
using System.Threading;
using BeyondTech.Extensions.Logging.Timing.Tests.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BeyondTech.Extensions.Logging.Timing.Tests
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
            using (var operation = logger.Logger.OperationAt(LogLevel.Debug, LogLevel.Critical, TimeSpan.FromMilliseconds(1000)).Begin(operationName))
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
            using (var operation = logger.Logger.OperationAt(LogLevel.Debug, LogLevel.Critical, TimeSpan.FromMilliseconds(1000)).Begin(operationName))
            {
                operation.Complete();
            }

            Assert.Single(logs);
            Assert.StartsWith("dbug:", logs[0]);
        }

        [Fact]
        public void OperationAt_LogsNothingIfLogLevelIsNotSpecified()
        {
            var logs = new List<string>();

            const string operationName = "Long Op!";

            using (var logger = new TailLogger(logs, LogLevel.Critical))
            using (var operation = logger.Logger.OperationAt(LogLevel.Debug, LogLevel.Debug, TimeSpan.FromMilliseconds(1000)).Begin(operationName))
            {
                operation.Complete();
            }

            Assert.Empty(logs);
        }

        [Fact]
        public void Operation_ThrowsWhenCompleteIsCalledWithANullTemplate()
        {
            const string operationName = "Long Op!";

            var logger = NullLogger<string>.Instance;
            using (var operation = logger.BeginOperation(operationName))
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                Assert.Throws<ArgumentNullException>(() => operation.Complete(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
        }

        [Fact]
        public void Operation_LogsMessageOnComplete()
        {
            var logs = new List<string>();

            const string operationName = "Long Op!";

            using (var logger = new TailLogger(logs))
            using (var operation = logger.Logger.OperationAt(LogLevel.Debug, LogLevel.Critical, TimeSpan.FromMilliseconds(1000)).Begin(operationName))
            {
                operation.Complete("This is complete!");
            }

            Assert.Single(logs);
            Assert.StartsWith("dbug:", logs[0]);
            Assert.Contains("This is complete", logs[0]);
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

        [Fact]
        public void Operation_SetExceptionAndRethrow_Throws()
        {
            var logs = new List<string>();

            Assert.Throws<NotImplementedException>(() =>
            {
                using (var logger = new TailLogger(logs))
                using (var op = logger.Logger.BeginOperation("Performing work"))
                {
                    try
                    {
                        throw new NotImplementedException();
                    }
                    catch (Exception ex) when (op.SetExceptionAndRethrow(ex))
                    {
                        // this line should never be hit
                        Assert.True(false);
                    }
                }
            });

            Assert.Single(logs);
            Assert.Contains("System.NotImplementedException", logs[0]);
        }

        [Fact]
        public void Operation_ThrowsWhenILoggerIsNull()
        {
            ILogger? logger = null;
#pragma warning disable CS8604 // Possible null reference argument.
            Assert.Throws<ArgumentNullException>(() => logger.OperationAt(LogLevel.Information));
            Assert.Throws<ArgumentNullException>(() => logger.BeginOperation(""));
            Assert.Throws<ArgumentNullException>(() => logger.TimeOperation(""));
#pragma warning restore CS8604 // Possible null reference argument.
        }

        [Fact]
        public void Operation_ThrowsWhenMessageTemplateIsNull()
        {
#pragma warning disable CS8604 // Possible null reference argument.
            string? messageTemplate = null;
            Assert.Throws<ArgumentNullException>(() => NullLogger<string>.Instance.BeginOperation(messageTemplate));
#pragma warning restore CS8604 // Possible null reference argument.
        }

        [Fact]
        public void Operation_ThrowsWhenArgsAreNull()
        {
#pragma warning disable CS8604 // Possible null reference argument.
            object[]? args = null;
            Assert.Throws<ArgumentNullException>(() => NullLogger<string>.Instance.BeginOperation("This is a template", args));
#pragma warning restore CS8604 // Possible null reference argument.
        }
    }
}