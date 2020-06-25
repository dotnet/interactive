// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Linq;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Server
{
    [LogTestNamesToPocketLogger]
    public class StandardIOKernelServerTests : IDisposable
    {
        private readonly StandardIOKernelServer _standardIOKernelServer;
        private readonly SubscribedList<IKernelEventEnvelope> _kernelEvents;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public StandardIOKernelServerTests(ITestOutputHelper output)
        {
            var kernel = new CompositeKernel
            {
                new CSharpKernel()
                    .UseKernelHelpers()
                    .UseNugetDirective()
                    .UseDefaultFormatting()
                    .UseDefaultMagicCommands()
            };

            _standardIOKernelServer = new StandardIOKernelServer(
                kernel,
                new StreamReader(new MemoryStream()),
                new StringWriter());

            _kernelEvents = _standardIOKernelServer
                            .Output
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(KernelEventEnvelope.Deserialize)
                            .ToSubscribedList();

            _disposables.Add(_standardIOKernelServer);
            _disposables.Add(output.SubscribeToPocketLogger());
            _disposables.Add(kernel.LogEventsToPocketLogger());
            _disposables.Add(kernel);
        }

        [Fact]
        public void The_server_is_started_after_creation()
        {
            _standardIOKernelServer
                .IsStarted
                .Should()
                .BeTrue();
        }


        [Fact]
        public async Task It_produces_a_unique_CommandHandled_for_root_command()
        {
            var command = new SubmitCode("#!time\ndisplay(1543); display(4567);");
            command.SetToken("abc");

            await _standardIOKernelServer.WriteAsync(command);

            _kernelEvents
                .Should()
                .ContainSingle<KernelEventEnvelope<CommandSucceeded>>()
                .Which
                .Event
                .Command
                .GetToken()
                .Should()
                .Be("abc");
        }

        [Fact]
        public async Task It_does_not_publish_ReturnValueProduced_events_if_the_value_is_DisplayedValue()
        {
            await _standardIOKernelServer.WriteAsync(new SubmitCode("display(1543)"));

            _kernelEvents
                .Should()
                .NotContain(e => e.Event is ReturnValueProduced);
        }

        [Fact]
        public async Task It_publishes_diagnostic_events_on_json_parse_errors()
        {
            var invalidJson = "{ hello";

            await _standardIOKernelServer.WriteAsync(invalidJson);

            _kernelEvents
                .Should()
                .ContainSingle<KernelEventEnvelope<DiagnosticLogEntryProduced>>()
                .Which
                .Event
                .Message
                .Should()
                .Contain(invalidJson);
        }

        [Fact]
        public async Task It_indicates_when_a_code_submission_is_incomplete()
        {
            var command = new SubmitCode(@"var a = 12");
            command.SetToken("abc");

            await _standardIOKernelServer.WriteAsync(command);

            _kernelEvents
                .Should()
                .ContainSingle<KernelEventEnvelope<IncompleteCodeSubmissionReceived>>(e => e.Event.Command.GetToken() == "abc");
        }

        [Fact]
        public async Task It_does_not_indicate_compilation_errors_as_exceptions()
        {
            var command = new SubmitCode("DOES NOT COMPILE");
            command.SetToken("abc");

            await _standardIOKernelServer.WriteAsync(command);

            _kernelEvents
                .Should()
                .ContainSingle<KernelEventEnvelope<CommandFailed>>()
                .Which
                .Event
                .Message
                .ToLowerInvariant()
                .Should()
                .NotContain("exception");
        }

        [Fact]
        public async Task It_can_eval_function_instances()
        {
            await _standardIOKernelServer.WriteAsync(new SubmitCode(@"Func<int> func = () => 1;"));

            await _standardIOKernelServer.WriteAsync(new SubmitCode(@"func()"));

            await _standardIOKernelServer.WriteAsync(new SubmitCode(@"func"));

            _kernelEvents
                .Count(e => e.Event is ReturnValueProduced)
                .Should()
                .Be(2);
        }

        [Fact]
        public async Task Kernel_can_pound_r_nuget_using_kernel_client()
        {
            var command = new SubmitCode(@"#r ""nuget:Microsoft.Spark, 0.4.0""");
            command.SetToken("abc");

            await _standardIOKernelServer.WriteAsync(command);

            _kernelEvents
                .Should()
                .ContainSingle<KernelEventEnvelope<PackageAdded>>(
                    where: e => e.Event.Command.GetToken() == "abc" && 
                                e.Event.PackageReference.PackageName == "Microsoft.Spark");
        }

        [Fact]
        public async Task it_produces_values_when_executing_Console_output()
        {
            var guid = Guid.NewGuid().ToString();

            var command = new SubmitCode($"Console.Write(\"{guid}\");");

            await _standardIOKernelServer.WriteAsync(command);

            _kernelEvents
                .Should()
                .ContainSingle<KernelEventEnvelope<StandardOutputValueProduced>>()
                .Which
                .Event
                .FormattedValues
                .Should()
                .ContainSingle(f => f.MimeType == PlainTextFormatter.MimeType &&
                                    f.Value.Equals(guid));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}