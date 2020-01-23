// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine
{
    public class FirstTimeUseSentinelTests : IDisposable
    {
        private readonly FileInfo _connectionFile;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public FirstTimeUseSentinelTests()
        {
            _connectionFile = new FileInfo(Path.GetTempFileName());

            _disposables.Add(() => _connectionFile.Delete());
        }

        private static Parser CreateParser(bool sentinelExists)
        {
            var firstTimeUseNoticeSentinel =
                new FirstTimeUseNoticeSentinel(
                    "product-version",
                    "",
                    (_) => sentinelExists,
                    (_) => true,
                    (_) => { },
                    (_) => { });

            return CommandLineParser.Create(
                new ServiceCollection(),
                startServer: (options, invocationContext) => { },
                jupyter: (startupOptions, console, startServer, context) => Task.FromResult(1),
                startKernelServer: (startupOptions, kernel, console) => Task.FromResult(1),
                telemetry: new FakeTelemetry(),
                firstTimeUseNoticeSentinel: firstTimeUseNoticeSentinel);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public async Task First_time_use_sentinel_does_not_exist_then_print_telemetry_first_time_use_welcome_message()
        {
            var console = new TestConsole();
            var parser = CreateParser(false);
            await parser.InvokeAsync($"jupyter {_connectionFile}", console);
            Assert.Contains("Telemetry", console.Out.ToString());
        }

        [Fact]
        public async Task First_time_use_sentinel_exists_then_do_not_print_telemetry_first_time_use_welcome_message()
        {
            var console = new TestConsole();
            var parser = CreateParser(true);
            await parser.InvokeAsync($"jupyter  {_connectionFile}", console);
            Assert.DoesNotContain("Telemetry", console.Out.ToString());
        }
    }
}