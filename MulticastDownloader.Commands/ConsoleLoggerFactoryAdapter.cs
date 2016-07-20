// <copyright file="ConsoleLoggerFactoryAdapter.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Commands
{
    using System;
    using System.IO;
    using Common.Logging;
    using Common.Logging.Simple;
    using PCLStorage;

    internal class ConsoleLoggerFactoryAdapter : ILoggerFactoryAdapter
    {
        private LogLevel logLevel;
        private string logFile;

        internal ConsoleLoggerFactoryAdapter(LogLevel logLevel, string logFile)
        {
            this.logLevel = logLevel;
            this.logFile = logFile;
        }

        public ILog GetLogger(string key)
        {
            return new ConsoleOutputLogger(key, this.logLevel, this.logFile);
        }

        public ILog GetLogger(Type type)
        {
            return new ConsoleOutputLogger(type.ToString(), this.logLevel, this.logFile);
        }

        private class ConsoleOutputLogger : AbstractSimpleLogger
        {
            private LogLevel logLevel;
            private string logFile;

            internal ConsoleOutputLogger(string key, LogLevel logLevel, string logFile)
                : base(key, logLevel, true, true, true, "HH:MM:ss")
            {
                this.logLevel = logLevel;
                this.logFile = logFile;
            }

            protected override async void WriteInternal(LogLevel level, object message, Exception exception)
            {
                TextWriter writer;
                if (level == LogLevel.Error || level == LogLevel.Fatal || level == LogLevel.Warn)
                {
                    writer = Console.Out;
                }
                else
                {
                    writer = Console.Error;
                }

                WriteOutput(level, message, exception, writer);

                if (this.logFile != null)
                {
                    IFile file = await FileSystem.Current.LocalStorage.CreateFileAsync(this.logFile, CreationCollisionOption.OpenIfExists);
                    using (Stream fileStream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
                    {
                        fileStream.Position = fileStream.Length;
                        using (StreamWriter fileStreamWriter = new StreamWriter(fileStream))
                        {
                            WriteOutput(level, message, exception, fileStreamWriter);
                        }
                    }
                }
            }

            private static void WriteOutput(LogLevel level, object message, Exception exception, TextWriter writer)
            {
                if (message != null)
                {
                    writer.WriteLine("[" + level.ToString() + "]: " + message);
                }

                if (exception != null)
                {
                    writer.WriteLine(exception.ToString());
                }
            }
        }
    }
}
