// <copyright file="SessionTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core;
    using Core.Cryptography;
    using Core.Server;
    using IO;
    using PCLStorage;
    using Session;
    using Xunit;
    using Xunit.Abstractions;
    using SIO = System.IO;

    public class SessionTest
    {
        public SessionTest(ITestOutputHelper outputHelper)
        {
            LogManager.Adapter = new TestOutputLoggerFactoryAdapter(LogLevel.All, outputHelper);
        }
    }
}
