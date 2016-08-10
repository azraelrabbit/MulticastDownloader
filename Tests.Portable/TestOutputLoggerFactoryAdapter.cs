// <copyright file="TestOutputLoggerFactoryAdapter.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests
{
    using System;
    using Common.Logging;
    using Common.Logging.Simple;
    using Xunit;
    using Xunit.Abstractions;

    internal class TestOutputLoggerFactoryAdapter : ILoggerFactoryAdapter
    {
        private LogLevel logLevel;
        private ITestOutputHelper outputHelper;

        internal TestOutputLoggerFactoryAdapter(LogLevel logLevel, ITestOutputHelper outputHelper)
        {
            this.logLevel = logLevel;
            this.outputHelper = outputHelper;
        }

        public ILog GetLogger(string key)
        {
            return new TestOutputLogger(key, this.logLevel, this.outputHelper);
        }

        public ILog GetLogger(Type type)
        {
            return new TestOutputLogger(type.ToString(), this.logLevel, this.outputHelper);
        }

        private class TestOutputLogger : AbstractSimpleLogger
        {
            private LogLevel logLevel;
            private ITestOutputHelper outputHelper;

            internal TestOutputLogger(string key, LogLevel logLevel, ITestOutputHelper outputHelper)
                : base(key, logLevel, true, true, true, "HH:MM:ss")
            {
                this.logLevel = logLevel;
                this.outputHelper = outputHelper;
            }

            protected override void WriteInternal(LogLevel level, object message, Exception exception)
            {
                try
                {
                    if (message != null)
                    {
                        this.outputHelper.WriteLine(DateTime.Now.ToString() + " [" + level.ToString() + "]: " + message);
                    }

                    if (exception != null)
                    {
                        this.outputHelper.WriteLine(exception.ToString());
                    }
                }
                catch (InvalidOperationException)
                {
                    // XUnit sometimes finishes the test case before we can output anything...
                }
            }
        }
    }
}
