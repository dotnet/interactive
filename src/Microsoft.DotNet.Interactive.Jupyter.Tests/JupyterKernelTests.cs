// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class JupyterKernelTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private CompositeDisposable _disposables = new();

    public JupyterKernelTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public void Dispose()
    {
        //_disposables.Dispose();
    }

    private CompositeKernel CreateKernelAsync(IJupyterKernelConnectionOptions options)
    {
        Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);

        var csharpKernel = new CSharpKernel();
        var kernel = new CompositeKernel { csharpKernel };
        kernel.DefaultKernelName = csharpKernel.Name;

        var jupyterKernelCommand = new ConnectJupyterKernelCommand();
        kernel.AddKernelConnector(jupyterKernelCommand.AddConnectionOptions(options));

        _disposables.Add(kernel);
        return kernel;
    }

    [Theory]
    [JupyterHttpTestData("python3")]
    [JupyterHttpTestData("ir")]
    [JupyterZMQTestData("python3")]
    [JupyterZMQTestData("ir")]
    public async Task can_connect_to_and_get_kernel_info_produced(JupyterConnectionTestData connectionData, string kernelSpecToTest)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecToTest} {connectionData.GetConnectionString()}");

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
    [JupyterHttpTestData("python3")]
    [JupyterZMQTestData("python3")]
    public async Task can_submit_code_and_get_return_value_produced(JupyterConnectionTestData connectionData, string kernelSpecToTest)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecToTest} {connectionData.GetConnectionString()}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync("#!testKernel\n1+1");
        var events = result.KernelEvents.ToSubscribedList();

        events
            .Should()
            .NotContainErrors();

        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest)
            .Which
            .Content
            .As<ExecuteRequest>()
            .Code
            .Trim()
            .Should()
            .Be("1+1");

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
    [JupyterHttpTestData("python3", "from IPython.display import display; display(2)", PlainTextFormatter.MimeType, "2")]
    [JupyterZMQTestData("python3", "from IPython.display import display; display(2)", PlainTextFormatter.MimeType, "2")]
    [JupyterHttpTestData("ir", "1+1", PlainTextFormatter.MimeType, "[1] 2")]
    [JupyterZMQTestData("ir", "1+1", PlainTextFormatter.MimeType, "[1] 2")]
    public async Task can_submit_code_and_get_display_value_produced(JupyterConnectionTestData connectionData, string kernelSpecToTest, string codeToRun, string mimeType, string outputReturned)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecToTest} {connectionData.GetConnectionString()}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync($"#!testKernel\n{codeToRun}");
        var events = result.KernelEvents.ToSubscribedList();

        events
            .Should()
            .NotContainErrors();

        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest)
            .Which
            .Content
            .As<ExecuteRequest>()
            .Code
            .Trim()
            .Should()
            .Be(codeToRun);


        events.Should()
           .ContainSingle<DisplayedValueProduced>()
           .Which
           .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == mimeType)
            .Which
            .Value
            .Trim()
            .Should()
            .Be(outputReturned);
    }
}
