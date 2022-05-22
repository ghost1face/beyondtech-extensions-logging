using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.Timing.Tests.Utilities
{
    public sealed class TailLogger : IDisposable
    {
        public ILogger Logger { get; }

        private readonly IDisposable _reader;

        public TailLogger(IList<string> logs)
        {
            _reader = ConsoleReader.Begin(logs);

            Logger = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                    });
            }).CreateLogger("TailLogger");
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }

}