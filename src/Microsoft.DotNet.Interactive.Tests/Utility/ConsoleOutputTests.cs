// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    [LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
    public class ConsoleOutputTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public ConsoleOutputTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public void Prior_console_output_is_restored_after_subscribers_have_disposed()
        {
            SubscribedList<string> stdOut = null;

            using var subscription = ConsoleOutput.Subscribe(console =>
            {
                stdOut = console.Out.ToSubscribedList();

                return Disposable.Empty;
            });

            Console.Out.Write("hello out!");

            subscription.Dispose();

            Console.Out.Write("goodbye out!");

            stdOut.Should().BeEquivalentTo("hello out!");
        }

        [Fact]
        public void Prior_console_error_is_restored_after_subscribers_have_disposed()
        {
            SubscribedList<string> stdErr = null;

            using var subscription = ConsoleOutput.Subscribe(console =>
            {
                stdErr = console.Error.ToSubscribedList();

                return Disposable.Empty;
            });

            Console.Error.Write("hello err!");

            subscription.Dispose();

            Console.Error.Write("goodbye err!");

            stdErr.Should().BeEquivalentTo("hello err!");
        }

        [Fact]
        public void Prior_console_output_is_not_restored_before_all_subscribers_have_disposed()
        {
            SubscribedList<string> stdOut1 = null;
            SubscribedList<string> stdOut2 = null;

            var subscription1 = ConsoleOutput.Subscribe(console =>
            {
                stdOut1 = console.Out.ToSubscribedList();

                return Disposable.Create(() => stdOut1.Dispose());
            });

            using var subscription2 = ConsoleOutput.Subscribe(console =>
            {
                stdOut2 = console.Out.ToSubscribedList();

                return Disposable.Create(() => stdOut2.Dispose());
            });

            Console.Out.Write("1");

            subscription1.Dispose();

            Console.Out.Write("2");

            stdOut1.Should().BeEquivalentTo("1");
            stdOut2.Should().BeEquivalentTo("1", "2");
        }
    }
}