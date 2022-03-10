// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Connection
{
    public class KernelHostTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly CompositeKernel _localCompositeKernel;
        private readonly CompositeKernel _remoteCompositeKernel;

        public KernelHostTests(ITestOutputHelper output)
        {
            _localCompositeKernel = new CompositeKernel();

            _remoteCompositeKernel = new CompositeKernel
            {
                new CSharpKernel()
                    .UseKernelHelpers()
                    .UseNugetDirective()
                    .UseDefaultMagicCommands()
            };

            ConnectHost.ConnectInProcessHost(
                _localCompositeKernel,
                new Uri("kernel://local/"),
                _remoteCompositeKernel,
                new Uri("kernel://remote/"));

            _localCompositeKernel
                .Host
                .ConnectProxyKernelOnDefaultConnectorAsync("csharp")
                .GetAwaiter().GetResult();

            _disposables.Add(output.SubscribeToPocketLogger());
            _disposables.Add(_remoteCompositeKernel.LogEventsToPocketLogger());
            _disposables.Add(_remoteCompositeKernel);
        }

        [Fact]
        public void When_kernel_is_added_to_hosted_CompositeKernel_then_origin_URI_is_set()
        {
            var kernel = new FakeKernel("fake");

            _localCompositeKernel.Add(kernel);

            var kernelInfo = kernel.KernelInfo;

            kernelInfo.Uri.Should().Be(new Uri(_localCompositeKernel.Host.Uri, "fake"));
        }

        // FIX: (KernelHostTests) figure out which of these tests are still important

        // [Fact]
        // public async Task It_produces_a_unique_CommandSucceeded_for_root_command()
        // {
        //     var command = new SubmitCode("#!time\ndisplay(1543); display(4567);");
        //
        //     var result = await _localCompositeKernel.SendAsync(command);
        //
        //     var events = result.KernelEvents.ToSubscribedList();
        //
        //     events
        //         .Should()
        //         .ContainSingle<CommandSucceeded>();
        // }



        //
        // [Fact]
        // public async Task It_does_not_publish_ReturnValueProduced_events_if_the_value_is_DisplayedValue()
        // {
        //     _commandAndEventSender.Send(new SubmitCode("display(1543)"));
        //
        //     await WaitForCompletion();
        //
        //     KernelEvents
        //         .Should()
        //         .NotContain(e => e.Event is ReturnValueProduced);
        // }
        //
        // [Fact]
        // public async Task It_indicates_when_a_code_submission_is_incomplete()
        // {
        //     var command = new SubmitCode(@"var a = 12");
        //     command.SetToken("abc");
        //
        //     _commandAndEventSender.Send(command);
        //
        //     await WaitForCompletion();
        //
        //     KernelEvents
        //         .Should()
        //         .ContainSingle<KernelEventEnvelope<IncompleteCodeSubmissionReceived>>(e => e.Event.Command.GetOrCreateToken() == "abc");
        // }
        //
        // [Fact]
        // public async Task It_does_not_indicate_compilation_errors_as_exceptions()
        // {
        //     var command = new SubmitCode("DOES NOT COMPILE");
        //     command.SetToken("abc");
        //
        //     _commandAndEventSender.Send(command);
        //
        //     await WaitForCompletion();
        //
        //     KernelEvents
        //         .Should()
        //         .ContainSingle<KernelEventEnvelope<CommandFailed>>()
        //         .Which
        //         .Event
        //         .Message
        //         .ToLowerInvariant()
        //         .Should()
        //         .NotContain("exception");
        // }
        //
        // [Fact]
        // public async Task It_can_eval_function_instances()
        // {
        //     _commandAndEventSender.Send(new SubmitCode(@"Func<int> func = () => 1;"));
        //
        //     await WaitForCompletion();
        //
        //     _commandAndEventSender.Send(new SubmitCode(@"func()"));
        //     var kernelCommand = new SubmitCode(@"func");
        //     kernelCommand.SetToken("finalCommand");
        //     _commandAndEventSender.Send(kernelCommand);
        //
        //     await WaitForCompletion("finalCommand");
        //
        //     KernelEvents
        //         .Count(e => e.Event is ReturnValueProduced)
        //         .Should()
        //         .Be(2);
        // }

        //
        // [Fact]
        // public async Task Kernel_can_pound_r_nuget_using_kernel_client()
        // {
        //     var command = new SubmitCode(@"#r ""nuget:Microsoft.Spark, 0.4.0""");
        //     command.SetToken("abc");
        //
        //     _commandAndEventSender.Send(command);
        //
        //     await WaitForCompletion();
        //
        //     KernelEvents
        //         .Should()
        //         .ContainSingle<KernelEventEnvelope<PackageAdded>>(
        //             @where: e => e.Event.Command.GetOrCreateToken() == "abc" &&
        //                         e.Event.PackageReference.PackageName == "Microsoft.Spark");
        // }
        //
        // [Fact]
        // public async Task it_produces_values_when_executing_Console_output()
        // {
        //     using var _ = await ConsoleLock.AcquireAsync();
        //
        //     var guid = Guid.NewGuid().ToString();
        //
        //     var command = new SubmitCode($"Console.Write(\"{guid}\");");
        //
        //     _commandAndEventSender.Send(command);
        //
        //     await WaitForCompletion();
        //
        //     KernelEvents
        //         .Should()
        //         .ContainSingle<KernelEventEnvelope<StandardOutputValueProduced>>()
        //         .Which
        //         .Event
        //         .FormattedValues
        //         .Should()
        //         .ContainSingle(f => f.MimeType == PlainTextFormatter.MimeType &&
        //                             f.Value.Equals(guid));
        // }

        public void Dispose() => _disposables.Dispose();
    }
}