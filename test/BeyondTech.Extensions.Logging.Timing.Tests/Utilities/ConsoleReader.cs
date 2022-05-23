using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BeyondTech.Extensions.Logging.Timing.Tests.Utilities
{
    internal class ConsoleReader : IDisposable
    {
        private readonly IList<string> _logs;
        private readonly TextWriter _backupOut;
        private readonly TextWriter _tempOut;

        public ConsoleReader(IList<string> logs)
        {
            _logs = logs;
            _backupOut = System.Console.Out;
            _tempOut = new StringWriter();

            System.Console.SetOut(_tempOut);
        }

        public static IDisposable Begin(IList<string> logs)
        {
            return new ConsoleReader(logs);
        }

        public void Dispose()
        {
            Thread.Sleep(100);

            FillLogs();

            _tempOut.Dispose();

            System.Console.SetOut(_backupOut);
        }

        private void FillLogs()
        {
            if (_logs.Any())
                return;

            var output = _tempOut.ToString() ?? string.Empty;

            StringBuilder? builder = null;
            Regex newLogMessageIdentifier = new("^(dbug|info|warn|fail|crit):");
            foreach (var o in output!.Split(Environment.NewLine))
            {
                if (newLogMessageIdentifier.IsMatch(o))
                {
                    if (builder != null)
                    {
                        _logs.Add(builder.ToString().Trim());
                    }

                    builder = new StringBuilder(o);
                }
                else
                {
                    builder?.Append(o);
                }
            }

            if (builder != null)
            {
                _logs.Add(builder.ToString().Trim());
            }
        }
    }

}