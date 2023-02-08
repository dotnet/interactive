// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
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

    // to re-record the tests for simulated playback with JuptyerTestData, set this to true
    private const bool RECORD_FOR_PLAYBACK = false;
    
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
    [JupyterHttpTestData(KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData(KernelSpecName = "ir", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = "python3")]
    [JupyterZMQTestData(KernelSpecName = "ir")]
    [JupyterTestData(KernelSpecName = "python3")]
    [JupyterTestData(KernelSpecName = "ir")]
    public async Task can_connect_to_and_setup_kernel(JupyterConnectionTestData connectionData)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

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

        var testKernel = kernel.FindKernelByName("testKernel");
        Assert.NotNull(testKernel);

        // kernel info should be sent as kernel info produced
        testKernel
            .KernelInfo
            .Should()
            .BeEquivalentTo(new
            {
                LanguageName = kernelInfoReturned.LanguageInfo.Name,
                LanguageVersion = kernelInfoReturned.LanguageInfo.Version
            }, c => c.ExcludingMissingMembers());

        // ensure variable sharing is setup
        testKernel
            .KernelInfo
            .SupportedKernelCommands
            .Contains(new KernelCommandInfo(nameof(RequestValue)));

        testKernel
            .KernelInfo
            .SupportedKernelCommands
            .Contains(new KernelCommandInfo(nameof(RequestValueInfos)));

        testKernel
            .KernelInfo
            .SupportedKernelCommands
            .Contains(new KernelCommandInfo(nameof(SendValue)));
        
        options.SaveState();
    }

    // note that R kernel returns display_data instead of execute_result
    [Theory]
    [JupyterHttpTestData(KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = "python3")]
    [JupyterTestData(KernelSpecName = "python3")]
    public async Task can_submit_code_and_get_return_value_produced(JupyterConnectionTestData connectionData)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

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

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("from IPython.display import display; display(2)", PlainTextFormatter.MimeType, "2", KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("from IPython.display import display; display(2)", PlainTextFormatter.MimeType, "2", KernelSpecName = "python3")]
    [JupyterTestData("from IPython.display import display; display(2)", PlainTextFormatter.MimeType, "2", KernelSpecName = "python3")]
    [JupyterHttpTestData("1+1", PlainTextFormatter.MimeType, "[1] 2", KernelSpecName = "ir", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("1+1", PlainTextFormatter.MimeType, "[1] 2", KernelSpecName = "ir")]
    [JupyterTestData("1+1", PlainTextFormatter.MimeType, "[1] 2", KernelSpecName = "ir")]
    public async Task can_submit_code_and_get_display_value_produced(JupyterConnectionTestData connectionData, string codeToRun, string mimeType, string outputReturned)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

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

        options.SaveState();
    }


}
