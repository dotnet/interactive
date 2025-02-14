// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Pocket.For.Xunit;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class JupyterKernelCommandTests : JupyterKernelTestBase
{
    public JupyterKernelCommandTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [JupyterHttpTestData("python", KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData("R", KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("python", KernelSpecName = PythonKernelName)]
    [JupyterZMQTestData("R", KernelSpecName = RKernelName)]
    [JupyterTestData("python", KernelSpecName = PythonKernelName)]
    [JupyterTestData("R", KernelSpecName = RKernelName)]
    public async Task can_connect_to_and_setup_kernel(JupyterConnectionTestData connectionData, string languageName)
    {
        using var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        using var receivedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var result = await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.ConnectionString}");

        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        // should check on the kernel
        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.KernelInfoRequest);

        var kernelInfoReturned = receivedMessages
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
    [JupyterHttpTestData("1+1", PlainTextFormatter.MimeType, "2", KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("1+1", PlainTextFormatter.MimeType, "2", KernelSpecName = PythonKernelName)]
    [JupyterTestData("1+1", PlainTextFormatter.MimeType, "2", KernelSpecName = PythonKernelName)]
    public async Task can_submit_code_and_get_return_value_produced(
        JupyterConnectionTestData connectionData, 
        string codeToRun, 
        string mimeType, 
        string outputReturned)
    {
        using var options = connectionData.GetConnectionOptions();

        using var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.ConnectionString}");

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        using var receivedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

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
    [JupyterHttpTestData("from IPython.display import display; display(2)", new[] { "text/plain" }, new[] { "2" }, KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("from IPython.display import display; display(2)", new[] { "text/plain" }, new[] { "2" }, KernelSpecName = PythonKernelName)]
    [JupyterTestData("from IPython.display import display; display(2)", new[] { "text/plain" }, new[] { "2" }, KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData("1+1", new[] { "text/plain", "text/html", "text/latex", "text/markdown" }, new[] { "[1] 2", "2", "2", "2" }, KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("1+1", new[] { "text/plain", "text/html", "text/latex", "text/markdown" }, new[] { "[1] 2", "2", "2", "2" }, KernelSpecName = RKernelName)]
    [JupyterTestData("1+1", new[] { "text/plain", "text/html", "text/latex", "text/markdown" }, new[] { "[1] 2", "2", "2", "2" }, KernelSpecName = RKernelName)]
    public async Task can_submit_code_and_get_display_value_produced(JupyterConnectionTestData connectionData, string codeToRun, string[] mimeTypes, string[] valuesToExpect)
    {
        using var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.ConnectionString}");

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        using var receivedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

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

        var displayValueProduced = events.Should()
           .ContainSingle<DisplayedValueProduced>()
           .Which;

        for (int i = 0; i < mimeTypes.Length; i++)
        {
            displayValueProduced
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
    [JupyterHttpTestData("for i in range(2):\n\tprint (i, flush=True)", new[] { "0\n", "1\n" }, KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("for i in range(2):\n\tprint (i, flush=True)", new[] { "0\n", "1\n" }, KernelSpecName = PythonKernelName)]
    [JupyterTestData("for i in range(2):\n\tprint (i, flush=True)", new[] { "0\n", "1\n" }, KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData("for (x in 1:2) {\n\tprint(x);\n\tflush.console()\n}", new[] { "[1] 1\n", "[1] 2\n" }, KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("for (x in 1:2) {\n\tprint(x);\n\tflush.console()\n}", new[] { "[1] 1\n", "[1] 2\n" }, KernelSpecName = RKernelName)]
    [JupyterTestData("for (x in 1:2) {\n\tprint(x);\n\tflush.console()\n}", new[] { "[1] 1\n", "[1] 2\n" }, KernelSpecName = RKernelName)]
    public async Task can_submit_code_and_get_stream_stdout_produced(JupyterConnectionTestData connectionData, string codeToRun, string[] outputReturned)
    {
        using var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.ConnectionString}");

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        using var receivedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

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
    [JupyterHttpTestData("import sys\n\nprint('stderr', file=sys.stderr)", "stderr\n", KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData("message('stderr')", "stderr\n", KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("import sys\n\nprint('stderr', file=sys.stderr)", "stderr\n", KernelSpecName = PythonKernelName)]
    [JupyterZMQTestData("message('stderr')", "stderr\n", KernelSpecName = RKernelName)]
    [JupyterTestData("import sys\n\nprint('stderr', file=sys.stderr)", "stderr\n", KernelSpecName = PythonKernelName)]
    [JupyterTestData("message('stderr')", "stderr\n", KernelSpecName = RKernelName)]
    public async Task can_submit_code_and_get_stderr_produced(JupyterConnectionTestData connectionData, string codeToRun, string outputReturned)
    {
        using var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.ConnectionString}");

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        using var receivedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

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
    [JupyterHttpTestData("prin()", new[] { "\u001B[1;31mNameError\u001B[0m: name 'prin' is not defined", "Traceback (most recent call last)" }, KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData("prin()", new[] { "Error in prin(): could not find function \"prin\"\nTraceback:\n" }, KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("prin()", new[] { "\u001B[1;31mNameError\u001B[0m: name 'prin' is not defined", "Traceback (most recent call last)" }, KernelSpecName = PythonKernelName)]
    [JupyterZMQTestData("prin()", new[] { "Error in prin(): could not find function \"prin\"\nTraceback:\n" }, KernelSpecName = RKernelName)]
    [JupyterTestData("prin()", new[] { "\u001B[1;31mNameError\u001B[0m: name 'prin' is not defined", "Traceback (most recent call last)" }, KernelSpecName = PythonKernelName)]
    [JupyterTestData("prin()", new[] { "Error in prin(): could not find function \"prin\"\nTraceback:\n" }, KernelSpecName = RKernelName)]
    public async Task can_submit_code_and_get_error_produced(JupyterConnectionTestData connectionData, string codeToRun, string[] errorMessages)
    {
        using var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.ConnectionString}");

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        using var receivedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

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
    [JupyterHttpTestData("dh = display(\"test\", display_id=True)\ndh.update(\"update-test\")", "'test'", "'update-test'", KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("dh = display(\"test\", display_id=True)\ndh.update(\"update-test\")", "'test'", "'update-test'", KernelSpecName = PythonKernelName)]
    [JupyterTestData("dh = display(\"test\", display_id=True)\ndh.update(\"update-test\")", "'test'", "'update-test'", KernelSpecName = PythonKernelName)]
    public async Task can_submit_code_and_get_update_display_produced(JupyterConnectionTestData connectionData, string codeToRun, string displayValue, string updateDisplayValue)
    {
        using var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.ConnectionString}");

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        using var receivedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

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
    [JupyterHttpTestData("print (\"test\")", 3, new[] { "text/plain" }, new[] { "Prints the values to a stream, or to sys.stdout by default.\n" }, KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("print (\"test\")", 3, new[] { "text/plain" }, new[] { "Prints the values to a stream, or to sys.stdout by default.\n" }, KernelSpecName = PythonKernelName)]
    [JupyterTestData("print (\"test\")", 3, new[] { "text/plain" }, new[] { "Prints the values to a stream, or to sys.stdout by default.\n" }, KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData("print (\"test\")", 3, new[] { "text/html", "text/latex", "text/plain" }, new[] { "<table width=\"100%\" summary=\"page for print {base}\"><tr><td>print {base}</td><td style=\"text-align: right;\">R Documentation</td>", "\\inputencoding{utf8}\n\\HeaderA{print}{Print Values}{print}\n", "print' prints its argument and returns it _invisibly_" }, KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("print (\"test\")", 3, new[] { "text/html", "text/latex", "text/plain" }, new[] { "<table width=\"100%\" summary=\"page for print {base}\"><tr><td>print {base}</td><td style=\"text-align: right;\">R Documentation</td>", "\\inputencoding{utf8}\n\\HeaderA{print}{Print Values}{print}\n", "print' prints its argument and returns it _invisibly_" }, KernelSpecName = RKernelName)]
    [JupyterTestData("print (\"test\")", 3, new[] { "text/html", "text/latex", "text/plain" }, new[] { "<table width=\"100%\" summary=\"page for print {base}\"><tr><td>print {base}</td><td style=\"text-align: right;\">R Documentation</td>", "\\inputencoding{utf8}\n\\HeaderA{print}{Print Values}{print}\n", "print' prints its argument and returns it _invisibly_" }, KernelSpecName = RKernelName)]
    public async Task can_request_hover_text_and_get_value_produced(JupyterConnectionTestData connectionData, string codeToInspect, int curPosition, string[] mimeTypes, string[] textValueSnippets)
    {
        using var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.ConnectionString);

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();

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

        for (int i = 0; i < mimeTypes.Length; i++)
        {
            hoverTextProduced
                .Which
                .Content
                .Should()
                .ContainSingle(v => v.MimeType == mimeTypes[i])
                .Which
                .Value
                .Should()
                .Contain(textValueSnippets[i]);
        }

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("print (\"test\")", 3, new[] { "text/plain" }, new[] { "Prints the values to a stream, or to sys.stdout by default.\n" }, KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("print (\"test\")", 3, new[] { "text/plain" }, new[] { "Prints the values to a stream, or to sys.stdout by default.\n" }, KernelSpecName = PythonKernelName)]
    [JupyterTestData("print (\"test\")", 3, new[] { "text/plain" }, new[] { "Prints the values to a stream, or to sys.stdout by default.\n" }, KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData("print (\"test\")", 3, new[] { "text/html", "text/latex", "text/plain" }, new[] { "<table width=\"100%\" summary=\"page for print {base}\"><tr><td>print {base}</td><td style=\"text-align: right;\">R Documentation</td>", "\\inputencoding{utf8}\n\\HeaderA{print}{Print Values}{print}\n", "print' prints its argument and returns it _invisibly_" }, KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("print (\"test\")", 3, new[] { "text/html", "text/latex", "text/plain" }, new[] { "<table width=\"100%\" summary=\"page for print {base}\"><tr><td>print {base}</td><td style=\"text-align: right;\">R Documentation</td>", "\\inputencoding{utf8}\n\\HeaderA{print}{Print Values}{print}\n", "print' prints its argument and returns it _invisibly_" }, KernelSpecName = RKernelName)]
    [JupyterTestData("print (\"test\")", 3, new[] { "text/html", "text/latex", "text/plain" }, new[] { "<table width=\"100%\" summary=\"page for print {base}\"><tr><td>print {base}</td><td style=\"text-align: right;\">R Documentation</td>", "\\inputencoding{utf8}\n\\HeaderA{print}{Print Values}{print}\n", "print' prints its argument and returns it _invisibly_" }, KernelSpecName = RKernelName)]
    public async Task can_request_signature_help_and_get_value_produced(JupyterConnectionTestData connectionData, string codeToInspect, int curPosition, string[] mimeTypes, string[] textValueSnippets)
    {
        using var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.ConnectionString);

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();

        var linePosition = SourceUtilities.GetPositionFromCursorOffset(codeToInspect, curPosition);
        var command = new RequestSignatureHelp(codeToInspect, linePosition);
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

        var signaturesProduced = events
             .Should()
             .ContainSingle<SignatureHelpProduced>()
             .Which
             .Signatures
             .Select(s => s.Documentation);

        signaturesProduced.Should().HaveCount(mimeTypes.Length);

        for (int i = 0; i < mimeTypes.Length; i++)
        {
            signaturesProduced
                .Should()
                .ContainSingle(v => v.MimeType == mimeTypes[i])
                .Which
                .Value
                .Should()
                .Contain(textValueSnippets[i]);
        }

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("pr", 0, 0, 2, new[] { "print", "property" }, KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("pr", 0, 0, 2, new[] { "print", "property" }, KernelSpecName = PythonKernelName)]
    [JupyterTestData("pr", 0, 0, 2, new[] { "print", "property" }, KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData("pr", 0, 0, 2, new[] { "print", "predict" }, KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("pr", 0, 0, 2, new[] { "print", "predict" }, KernelSpecName = RKernelName)]
    [JupyterTestData("pr", 0, 0, 2, new[] { "print", "predict" }, KernelSpecName = RKernelName)]
    public async Task can_request_completions_and_get_value_produced(JupyterConnectionTestData connectionData, string codeToInspect, int linePos, int startPos, int curPosition, string[] exampleMatches)
    {
        using var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.ConnectionString);

        using var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        using var receivedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        var linePosition = SourceUtilities.GetPositionFromCursorOffset(codeToInspect, curPosition);
        var command = new RequestCompletions(codeToInspect, linePosition);
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        var request = sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.CompleteRequest)
            .Which
            .Content
            .As<CompleteRequest>();

        request
            .Code
            .Should()
            .Be(codeToInspect);

        request
            .CursorPosition
            .Should()
            .Be(curPosition);

        var completionsProduced = events
             .Should()
             .ContainSingle<CompletionsProduced>();

        completionsProduced
            .Which
            .LinePositionSpan
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(
                    new LinePositionSpan(
                            new LinePosition(linePos, startPos),
                            new LinePosition(linePos, curPosition)));

        completionsProduced
            .Which
            .Completions
            .Select(c => c.DisplayText)
            .Should()
            .Contain(exampleMatches);

        var completionsFromKernel = receivedMessages
            .Where(m => m.Header.MessageType == JupyterMessageContentTypes.CompleteReply)
            .FirstOrDefault()
            .Content
            .As<CompleteReply>();

        completionsProduced
            .Which
            .Completions
            .Select(c => c.DisplayText)
            .Should()
            .Contain(completionsFromKernel.Matches);

        completionsProduced
           .Which
           .Completions
           .Select(c => c.InsertText)
           .Should()
           .Contain(completionsFromKernel.Matches);

        options.SaveState();
    }
}
