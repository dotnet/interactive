﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public partial class JupyterKernelTests : IDisposable
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
    [JupyterHttpTestData("python", KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData("R", KernelSpecName = "ir", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("python", KernelSpecName = "python3")]
    [JupyterZMQTestData("R", KernelSpecName = "ir")]
    [JupyterTestData("python", KernelSpecName = "python3")]
    [JupyterTestData("R", KernelSpecName = "ir")]
    public async Task can_connect_to_and_setup_kernel(JupyterConnectionTestData connectionData, string languageName)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        var events = result.Events;

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

        kernelInfoReturned
            .LanguageInfo
            .Name
            .Should()
            .Be(languageName);

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
        testKernel.KernelInfo.SupportedKernelCommands
            .Should().Contain(new KernelCommandInfo(nameof(RequestValue)));

        testKernel.KernelInfo.SupportedKernelCommands
            .Should().Contain(new KernelCommandInfo(nameof(RequestValueInfos)));

        testKernel.KernelInfo.SupportedKernelCommands
            .Should().Contain(new KernelCommandInfo(nameof(SendValue)));

        var directives = testKernel.KernelInfo.SupportedDirectives.Select(info => info.Name);
        directives.Should().Contain("#!who");
        directives.Should().Contain("#!whos");

        options.SaveState();
    }

    // note that R kernel returns display_data instead of execute_result
    [Theory]
    [JupyterHttpTestData("1+1", PlainTextFormatter.MimeType, "2", KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("1+1", PlainTextFormatter.MimeType, "2", KernelSpecName = "python3")]
    [JupyterTestData("1+1", PlainTextFormatter.MimeType, "2", KernelSpecName = "python3")]
    public async Task can_submit_code_and_get_return_value_produced(JupyterConnectionTestData connectionData, string codeToRun, string mimeType, string outputReturned)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync($"#!testKernel\n{codeToRun}");
        var events = result.Events;

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
            .Should()
            .Be(codeToRun);

        events.Should()
           .ContainSingle<ReturnValueProduced>()
           .Which
           .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == mimeType)
            .Which
            .Value
            .Should()
            .Be(outputReturned);

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("from IPython.display import display; display(2)", new[] { "text/plain" }, new[] { "2" }, KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("from IPython.display import display; display(2)", new[] { "text/plain" }, new[] { "2" }, KernelSpecName = "python3")]
    [JupyterTestData("from IPython.display import display; display(2)", new[] { "text/plain" }, new[] { "2" }, KernelSpecName = "python3")]
    [JupyterHttpTestData("1+1", new[] { "text/plain", "text/html", "text/latex", "text/markdown" }, new[] { "[1] 2", "2", "2", "2" }, KernelSpecName = "ir", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("1+1", new[] { "text/plain", "text/html", "text/latex", "text/markdown" }, new[] { "[1] 2", "2", "2", "2" }, KernelSpecName = "ir")]
    [JupyterTestData("1+1", new[] { "text/plain", "text/html", "text/latex", "text/markdown" }, new[] { "[1] 2", "2", "2", "2" }, KernelSpecName = "ir")]
    public async Task can_submit_code_and_get_display_value_produced(JupyterConnectionTestData connectionData, string codeToRun, string[] mimeTypes, string[] valuesToExpect)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync($"#!testKernel\n{codeToRun}");
        var events = result.Events;

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
            .Should()
            .Be(codeToRun);


        for (int i = 0; i < mimeTypes.Length; i++)
        {
            events.Should()
           .ContainSingle<DisplayedValueProduced>()
           .Which
           .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == mimeTypes[i])
            .Which
            .Value
            .Should()
            .Be(valuesToExpect[i]);
        }

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("for i in range(2):\n\tprint (i, flush=True)", new[] { "0\n", "1\n" }, KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("for i in range(2):\n\tprint (i, flush=True)", new[] { "0\n", "1\n" }, KernelSpecName = "python3")]
    [JupyterTestData("for i in range(2):\n\tprint (i, flush=True)", new[] { "0\n", "1\n" }, KernelSpecName = "python3")]
    [JupyterHttpTestData("for (x in 1:2) {\n\tprint(x);\n\tflush.console()\n}", new[] { "[1] 1\n", "[1] 2\n" }, KernelSpecName = "ir", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("for (x in 1:2) {\n\tprint(x);\n\tflush.console()\n}", new[] { "[1] 1\n", "[1] 2\n" }, KernelSpecName = "ir")]
    [JupyterTestData("for (x in 1:2) {\n\tprint(x);\n\tflush.console()\n}", new[] { "[1] 1\n", "[1] 2\n" }, KernelSpecName = "ir")]
    public async Task can_submit_code_and_get_stream_stdout_produced(JupyterConnectionTestData connectionData, string codeToRun, string[] outputReturned)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync($"#!testKernel\n{codeToRun}");
        var events = result.Events;

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
            .Should()
            .Be(codeToRun);

        events.OfType<StandardOutputValueProduced>()
            .Should()
            .HaveCount(outputReturned.Length);

        for (int i = 0; i < outputReturned.Length; i++)
        {
            events.OfType<StandardOutputValueProduced>()
                .ElementAt(i)
                .FormattedValues
                .Should()
                .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
                .Which
                .Value
                .Should()
                .Be(outputReturned[i]);
        }

        options.SaveState();
    }


    [Theory]
    [JupyterHttpTestData("import sys\n\nprint('stderr', file=sys.stderr)", "stderr\n", KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData("message('stderr')", "stderr\n", KernelSpecName = "ir", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("import sys\n\nprint('stderr', file=sys.stderr)", "stderr\n", KernelSpecName = "python3")]
    [JupyterZMQTestData("message('stderr')", "stderr\n", KernelSpecName = "ir")]
    [JupyterTestData("import sys\n\nprint('stderr', file=sys.stderr)", "stderr\n", KernelSpecName = "python3")]
    [JupyterTestData("message('stderr')", "stderr\n", KernelSpecName = "ir")]
    public async Task can_submit_code_and_get_stderr_produced(JupyterConnectionTestData connectionData, string codeToRun, string outputReturned)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync($"#!testKernel\n{codeToRun}");
        var events = result.Events;

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
            .Should()
            .Be(codeToRun);

        events.Should()
           .ContainSingle<StandardErrorValueProduced>()
           .Which
           .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
            .Which
            .Value
            .Should()
            .Be(outputReturned);

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("prin()", new[] { "\u001B[1;31mNameError\u001B[0m: name 'prin' is not defined", "Traceback (most recent call last)" }, KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData("prin()", new[] { "Error in prin(): could not find function \"prin\"\nTraceback:\n" }, KernelSpecName = "ir", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("prin()", new[] { "\u001B[1;31mNameError\u001B[0m: name 'prin' is not defined", "Traceback (most recent call last)" }, KernelSpecName = "python3")]
    [JupyterZMQTestData("prin()", new[] { "Error in prin(): could not find function \"prin\"\nTraceback:\n" }, KernelSpecName = "ir")]
    [JupyterTestData("prin()", new[] { "\u001B[1;31mNameError\u001B[0m: name 'prin' is not defined", "Traceback (most recent call last)" }, KernelSpecName = "python3")]
    [JupyterTestData("prin()", new[] { "Error in prin(): could not find function \"prin\"\nTraceback:\n" }, KernelSpecName = "ir")]
    public async Task can_submit_code_and_get_error_produced(JupyterConnectionTestData connectionData, string codeToRun, string[] errorMessages)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync($"#!testKernel\n{codeToRun}");
        var events = result.Events;

        events
            .Should()
            .Contain(e => e is CommandFailed);

        var request = sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest)
            .Which
            .Content
            .As<ExecuteRequest>();

        request
            .Code
            .Should()
            .Be(codeToRun);

        request
            .StopOnError
            .Should().BeTrue();

        // should be returning ErrorProduced but there is a bug on the front end
        // using stderr to display errors to the user
        events.Should()
           .ContainSingle<StandardErrorValueProduced>()
           .Which
           .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
            .Which
            .Value
            .Should()
            .ContainAll(errorMessages);

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("dh = display(\"test\", display_id=True)\ndh.update(\"update-test\")", "'test'", "'update-test'", KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("dh = display(\"test\", display_id=True)\ndh.update(\"update-test\")", "'test'", "'update-test'", KernelSpecName = "python3")]
    [JupyterTestData("dh = display(\"test\", display_id=True)\ndh.update(\"update-test\")", "'test'", "'update-test'", KernelSpecName = "python3")]
    public async Task can_submit_code_and_get_update_display_produced(JupyterConnectionTestData connectionData, string codeToRun, string displayValue, string updateDisplayValue)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync($"#!testKernel\n{codeToRun}");
        var events = result.Events;


        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest)
            .Which
            .Content
            .As<ExecuteRequest>()
            .Code
            .Should()
            .Be(codeToRun);

        var display = events
            .Should()
            .ContainSingle<DisplayedValueProduced>();

        display
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
            .Which
            .Value
            .Should()
            .Be(displayValue);

        var updateDisplay = events
            .Should()
            .ContainSingle<DisplayedValueUpdated>();

        updateDisplay
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
            .Which
            .Value
            .Should()
            .Be(updateDisplayValue);

        Assert.NotNull(display.Which.ValueId);

        updateDisplay
            .Which
            .ValueId
            .Should()
            .Be(display.Which.ValueId);

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("print (\"test\")", 3, "Docstring:\nprint(value, ..., sep=' ', end='\\n', file=sys.stdout, flush=False)\n\nPrints the values to a stream, or to sys.stdout by default.\n", KernelSpecName = "python3", AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("print (\"test\")", 3, "Docstring:\nprint(value, ..., sep=' ', end='\\n', file=sys.stdout, flush=False)\n\nPrints the values to a stream, or to sys.stdout by default.\n", KernelSpecName = "python3")]
    [JupyterTestData("print (\"test\")", 3, "Docstring:\nprint(value, ..., sep=' ', end='\\n', file=sys.stdout, flush=False)\n\nPrints the values to a stream, or to sys.stdout by default.\n", KernelSpecName = "python3")]
    public async Task can_request_hover_text_and_get_value_produced(JupyterConnectionTestData connectionData, string codeToInspect, int curPosition, string textValueSnippet)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.GetConnectionString());

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();

        var linePosition = SourceUtilities.GetPositionFromCursorOffset(codeToInspect, curPosition);
        var command = new RequestHoverText(codeToInspect, linePosition);
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        var request = sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.InspectRequest)
            .Which
            .Content
            .As<InspectRequest>();

        request
            .Code
            .Should()
            .Be(codeToInspect);

        request
            .CursorPos
            .Should()
            .Be(curPosition);

        var hoverTextProduced = events.Should()
           .ContainSingle<HoverTextProduced>();

        hoverTextProduced
            .Which
            .LinePositionSpan
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(new LinePositionSpan(linePosition, linePosition));

        hoverTextProduced
            .Which
            .Content
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
            .Which
            .Value
            .Should()
            .Contain(textValueSnippet);

        options.SaveState();
    }
}
