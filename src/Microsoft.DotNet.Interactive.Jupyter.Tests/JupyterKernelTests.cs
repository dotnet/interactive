// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Assent;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Xunit;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class JupyterKernelTests : IDisposable
{
    private readonly Configuration _configuration;
    private const bool SkipConnectionTests = true;
    private const string SkipReason = SkipConnectionTests ? "Setup not available" : null;
    private CompositeDisposable _disposables = new();

    public JupyterKernelTests()
    {
        _configuration = new Configuration()
            .UsingExtension("json");

        _configuration = _configuration.SetInteractive(Debugger.IsAttached);
    }

    public void Dispose()
    {
        //_disposables.Dispose();
    }

    private CompositeKernel CreateKernelAsync(IJupyterKernelConnectionOptions options)
    {
        Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);
        var csharpKernel = new CSharpKernel();

        var kernel = new CompositeKernel
        {
            csharpKernel,
        };
        _disposables.Add(kernel);

        kernel.DefaultKernelName = csharpKernel.Name;

        var jupyterKernelCommand = new ConnectJupyterKernelCommand();

        kernel.AddKernelConnector(jupyterKernelCommand.AddConnectionOptions(options));
        return kernel;
    }

    [Theory]
    [InlineData(typeof(JupyterHttpKernelConnectionOptions), "python3", Skip = SkipReason)]
    [InlineData(typeof(JupyterHttpKernelConnectionOptions), "ir", Skip = SkipReason)]
    [InlineData(typeof(JupyterLocalKernelConnectionOptions), "python3", Skip = SkipReason)]
    [InlineData(typeof(JupyterLocalKernelConnectionOptions), "ir", Skip = SkipReason)]
    public async Task can_connect_to_jupyter_kernel_and_kernel_info_produced(Type connectionOptionsToTest, string kernelSpecToTest)
    {
        var options = JupyterKernelTestHelper.GetConnectionOptions(connectionOptionsToTest);

        var kernel = CreateKernelAsync(options);

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecToTest} {options.TestConnectionString}");

        var events = result.KernelEvents.ToSubscribedList();

        events
            .Should()
            .NotContainErrors();

        // should check on the kernel
        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.KernelInfoRequest);

        var kernelInfoReturned = recievedMessages
            .Where(m => m.Header.MessageType == JupyterMessageContentTypes.KernelInfoReply)
            .Select(m => m.Content)
            .Cast<KernelInfoReply>()
            .First();

        // kernel info should be sent as kernel info produced
        events
            .Should()
            .ContainSingle<KernelInfoProduced>(e => e.KernelInfo.LocalName == "testKernel")
            .Which
            .KernelInfo
            .Should()
            .BeEquivalentTo(new
            {
                LanguageName = kernelInfoReturned.LanguageInfo.Name,
                LanguageVersion = kernelInfoReturned.LanguageInfo.Version
            }, c => c.ExcludingMissingMembers());

        // should send the comm message for setting up variable sharing channel
        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest);

        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.CommOpen);
    }

    // note that R kernel returns display_data instead of execute_result
    [Theory]
    [InlineData(typeof(JupyterHttpKernelConnectionOptions), "python3", Skip = SkipReason)]
    [InlineData(typeof(JupyterLocalKernelConnectionOptions), "python3", Skip = SkipReason)]
    public async Task can_submit_code_and_get_return_value_produced(Type connectionOptionsToTest, string kernelSpecToTest)
    {
        var options = JupyterKernelTestHelper.GetConnectionOptions(connectionOptionsToTest);

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecToTest} {options.TestConnectionString}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync("#!testKernel\n1+1");
        var events = result.KernelEvents.ToSubscribedList();

        events
            .Should()
            .NotContainErrors();

        // should send the comm message for setting up variable sharing channel
        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest);

        events.Should()
           .ContainSingle<ReturnValueProduced>()
           .Which
           .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
            .Which
            .Value
            .Trim()
            .Should()
            .Be("2");
    }

    [Theory]
    [InlineData(typeof(JupyterHttpKernelConnectionOptions), "python3", "from IPython.display import display; display(2)", Skip = SkipReason)]
    [InlineData(typeof(JupyterLocalKernelConnectionOptions), "python3", "from IPython.display import display; display(2)", Skip = SkipReason)]
    public async Task can_submit_code_and_get_display_value_produced(Type connectionOptionsToTest, string kernelSpecToTest, string codeToRun)
    {
        var options = JupyterKernelTestHelper.GetConnectionOptions(connectionOptionsToTest);

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecToTest} {options.TestConnectionString}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync($"#!testKernel\n{codeToRun}");
        var events = result.KernelEvents.ToSubscribedList();

        events
            .Should()
            .NotContainErrors();

        // should send the comm message for setting up variable sharing channel
        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest);
        
        events.Should()
           .ContainSingle<DisplayedValueProduced>()
           .Which
           .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
            .Which
            .Value
            .Trim()
            .Should()
            .Be("2");
    }
}
