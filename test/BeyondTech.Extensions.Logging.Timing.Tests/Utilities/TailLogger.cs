﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace BeyondTech.Extensions.Logging.Timing.Tests.Utilities
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