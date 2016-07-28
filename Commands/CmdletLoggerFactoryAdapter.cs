// <copyright file="CmdletLoggerFactoryAdapter.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Commands
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Management.Automation;
    using System.Text;
    using Common.Logging;
    using Common.Logging.Simple;
    using PCLStorage;

    internal class CmdletLoggerFactoryAdapter : ILoggerFactoryAdapter, IDisposable
    {
        private AsyncCmdlet cmdlet;
        private StreamWriter logFile;
        private bool disposedValue = false;

        internal CmdletLoggerFactoryAdapter(AsyncCmdlet cmdlet)
        {
            Contract.Requires(cmdlet != null);
            this.cmdlet = cmdlet;
            if (!string.IsNullOrEmpty(cmdlet.LogFile))
            {
                this.logFile = new StreamWriter(cmdlet.LogFile);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        public ILog GetLogger(string key)
        {
            return new ConsoleOutputLogger(key, this.cmdlet, this.logFile);
        }

        public ILog GetLogger(Type type)
        {
            return new ConsoleOutputLogger(type.ToString(), this.cmdlet, this.logFile);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                this.disposedValue = true;
                if (disposing)
                {
                    if (this.logFile != null)
                    {
                        this.logFile.Close();
                        this.logFile.Dispose();
                    }
                }
            }
        }

        private class ConsoleOutputLogger : AbstractSimpleLogger
        {
            private AsyncCmdlet cmdlet;
#pragma warning disable CA2213
            private StreamWriter logFile;
#pragma warning restore CA2213
            private string key;

            internal ConsoleOutputLogger(string key, AsyncCmdlet cmdlet, StreamWriter logFile)
                : base(key, cmdlet.LogLevel, true, true, true, "HH:MM:ss")
            {
                this.key = key;
                this.cmdlet = cmdlet;
                this.logFile = logFile;
            }

            protected override void WriteInternal(LogLevel level, object message, Exception exception)
            {
                this.cmdlet.UiInvoke((c) => this.WriteCmdlet(c, level, message, exception))
                           .Wait();

                if (this.logFile != null)
                {
                    string logText = ToString(level, message, exception);
                    this.logFile.WriteLine(logText);
                }
            }

            private static string ToString(LogLevel level, object message, Exception exception)
            {
                StringBuilder builder = new StringBuilder();
                if (message != null)
                {
                    builder.Append("[" + level.ToString() + "]: " + message);
                }

                if (exception != null)
                {
                    if (message != null)
                    {
                        builder.AppendLine();
                    }

                    builder.Append(exception.ToString());
                }

                return builder.ToString();
            }

            private void WriteCmdlet(Cmdlet c, LogLevel level, object message, Exception exception)
            {
                if (level == LogLevel.Warn || level == LogLevel.Error || level == LogLevel.Fatal)
                {
                    if (message != null)
                    {
                        c.WriteWarning(message.ToString());
                    }

                    if (exception != null)
                    {
                        c.WriteError(new ErrorRecord(exception, this.key, ErrorCategory.InvalidOperation, c));
                    }
                }
                else if (level == LogLevel.Debug)
                {
                    if (message != null)
                    {
                        c.WriteDebug(message.ToString());
                    }

                    if (exception != null)
                    {
                        c.WriteDebug(exception.ToString());
                    }
                }
                else if (level == LogLevel.Trace)
                {
                    if (message != null)
                    {
                        c.WriteVerbose(message.ToString());
                    }

                    if (exception != null)
                    {
                        c.WriteVerbose(exception.ToString());
                    }
                }
                else
                {
                    if (message != null)
                    {
                        c.WriteInformation(new InformationRecord(message.ToString(), this.key));
                    }

                    if (exception != null)
                    {
                        c.WriteInformation(new InformationRecord(exception, this.key));
                    }
                }
            }
        }
    }
}
