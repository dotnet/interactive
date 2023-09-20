// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class JupyterKernelTests : JupyterKernelTestBase
{
    [Fact]
    public async Task can_get_help_for_connect_jupyter()
    {
        var kernel = CreateCompositeKernelAsync(new JupyterHttpKernelConnectionOptions(), new JupyterLocalKernelConnectionOptions());
        
        var command = new SubmitCode("#!connect jupyter --help");

        var result = await kernel.SendAsync(command);

        result.Events
            .Should()
            .NotContain(e => e is CommandFailed);

        var outputs = result.Events.OfType<StandardOutputValueProduced>();

        outputs.Should().HaveCount(1);

        string.Join("",
                outputs
                    .SelectMany(e => e.FormattedValues.Select(v => v.Value))
            ).ToLowerInvariant()
            .Should()
            .ContainAll("--kernel-spec", "--init-script", "--url", "--token", "in preview");
    }

    [Fact]
    public async Task variable_sharing_not_enabled_for_unsupported_languages()
    {
        var options = new TestJupyterConnectionOptions(GenerateReplies(null, "unsupportedLanguage"));

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var kernel = await CreateJupyterKernelAsync(options);

        sentMessages
            .Select(m => m.Header.MessageType)
            .Should()
            .NotContain(JupyterMessageContentTypes.ExecuteRequest);

        kernel.KernelInfo.SupportedKernelCommands
            .Should().NotContain(new KernelCommandInfo(nameof(RequestValue)));

        kernel.KernelInfo.SupportedKernelCommands
            .Should().NotContain(new KernelCommandInfo(nameof(RequestValueInfos)));

        kernel.KernelInfo.SupportedKernelCommands
            .Should().NotContain(new KernelCommandInfo(nameof(SendValue)));

        var directives = kernel.KernelInfo.SupportedDirectives.Select(info => info.Name);
        directives.Should().NotContain("#!who");
        directives.Should().NotContain("#!whos");
    }

    [Fact]
    public async Task variable_sharing_not_enabled_for_when_target_not_found()
    {
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new ExecuteReplyOk(), Message.Create(new ExecuteRequest("target_setup"))),
                Message.Create(new CommClose("commId"), Message.Create(new CommOpen("commId", "target_setup", null)).Header)
        }, "python"));

        var kernel = await CreateJupyterKernelAsync(options);

        kernel.KernelInfo.SupportedKernelCommands
            .Should().NotContain(new KernelCommandInfo(nameof(RequestValue)));

        kernel.KernelInfo.SupportedKernelCommands
            .Should().NotContain(new KernelCommandInfo(nameof(RequestValueInfos)));

        kernel.KernelInfo.SupportedKernelCommands
            .Should().NotContain(new KernelCommandInfo(nameof(SendValue)));

        var directives = kernel.KernelInfo.SupportedDirectives.Select(info => info.Name);
        directives.Should().NotContain("#!who");
        directives.Should().NotContain("#!whos");
    }

    [Fact]
    public async Task can_setup_kernel_using_script()
    {
        string initScript = "kernel_setup_script";
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new ExecuteReplyOk(), Message.Create(new ExecuteRequest(initScript))),
        }));

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var kernel = await CreateJupyterKernelAsync(options, null, $"--init-script {initScript}");

        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest)
            .Which
            .Content
            .As<ExecuteRequest>()
            .Code
            .Should()
            .Be(initScript);
    }

    [Fact]
    public void can_get_a_list_of_kernelspecs_from_completions()
    {
        var specs = new List<KernelSpec>
        {
            new KernelSpec() { Name = "testKernelSpec", DisplayName = "Test Kernel Spec", Language = "testLanguage" },
            new KernelSpec() { Name = "sampleSpec", DisplayName = "Sample Spec", Language = "sampleLanguage" }
        };

        var options = new TestJupyterConnectionOptions(new TestJupyterConnection(new TestJupyterKernelConnection(null), specs));
        var jupyterKernelCommand = new ConnectJupyterKernelCommand();
        jupyterKernelCommand.AddConnectionOptions(options);

        var kernelSpecCompletions = jupyterKernelCommand.KernelSpecName.GetCompletions();
        kernelSpecCompletions
            .Should()
            .BeEquivalentTo(specs.Select(s => new System.CommandLine.Completions.CompletionItem(s.Name)));
    }

    [Fact]
    public async Task jupyter_and_kernel_connection_is_disposed_on_dispose()
    {
        var options = new TestJupyterConnectionOptions(GenerateReplies());
        var kernel = CreateCompositeKernelAsync(options);

        var result = await kernel.SubmitCodeAsync($"#!connect jupyter --kernel-name testKernel --kernel-spec testKernelSpec");

        result.Events
            .Should()
            .NotContainErrors();
        
        options.Connection.IsDisposed.Should().BeFalse();
        options.Connection.KernelConnection.IsDisposed.Should().BeFalse();
        
        kernel.Dispose();

        options.Connection.IsDisposed.Should().BeTrue();
        options.Connection.KernelConnection.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task can_cancel_submit_code_and_interrupt_kernel()
    {
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new InterruptReply(), Message.Create(new InterruptRequest())),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var waitForCommandReceieved = options.MessageTracker.SentMessages
                    .TakeUntil(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest)
                    .ToTask();

        var cts = new CancellationTokenSource();
        var request = new SubmitCode("test");
        var codeSubmissionTask = kernel.SendAsync(request, cts.Token);

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        await waitForCommandReceieved;
        cts.Cancel();

        // wait until task is done
        await codeSubmissionTask.ContinueWith(t => { },
            new CancellationToken(),
            TaskContinuationOptions.OnlyOnCanceled,
            TaskScheduler.Default);

        codeSubmissionTask
            .IsCanceled
            .Should()
            .BeTrue();

        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.InterruptRequest);
    }


    [Fact]
    public async Task submit_code_line_endings_are_normalized_to_LF()
    {
        string code = "\r\ntest\r\ncode\r\n\n";

        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
            Message.CreateReply(new ExecuteReplyOk(), Message.Create(new ExecuteRequest(code)))
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var result = await kernel.SubmitCodeAsync(code);

        sentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest)
            .Which
            .Content
            .As<ExecuteRequest>()
            .Code
            .Should()
            .Be("\ntest\ncode\n\n");
    }

    [Fact]
    public async Task can_fail_on_execute_reply_status_error()
    {
        string code = "test";
        var request = Message.Create(new ExecuteRequest(code));
        var replies = GenerateReplies();
        replies.AddRange(new[] {
                Message.CreatePubSub(new Status(StatusValues.Busy), request, "id"),
                Message.CreatePubSub(new Stream(Stream.StandardOutput, "line1"), request),
                Message.CreatePubSub(new DisplayData("source", new Dictionary<string, object> { { "text/plain", "line2"}  }), request),
                Message.CreateReply(new ExecuteReply(StatusValues.Error), request),
                Message.CreatePubSub(new Stream(Stream.StandardError, "line2"), request),
                Message.CreatePubSub(new Status(StatusValues.Idle), request, "id"),
        });
        var options = new TestJupyterConnectionOptions(replies);

        var kernel = await CreateJupyterKernelAsync(options);

        var result = await kernel.SubmitCodeAsync(code);
        var events = result.Events;

        events
            .Should()
            .Contain(e => e is CommandFailed);

        // command should fail but it should still process messages sent for the request until idle
        events.Should()
            .ContainSingle<StandardOutputValueProduced>();
        events.Should()
            .ContainSingle<DisplayedValueProduced>();
        events.Should()
            .ContainSingle<StandardErrorValueProduced>();
    }

    [Fact]
    public async Task can_cancel_hover_text_without_kernel_interrupt()
    {
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
            Message.CreateReply(new InterruptReply(), Message.Create(new InterruptRequest()))
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var waitForCommandReceived = options.MessageTracker.SentMessages
                    .TakeUntil(m => m.Header.MessageType == JupyterMessageContentTypes.InspectRequest)
                    .ToTask();

        var cts = new CancellationTokenSource();
        var request = new RequestHoverText("test", new LinePosition(0, 1));
        var requestHoverTextTask = kernel.SendAsync(request, cts.Token);

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        await waitForCommandReceived;
        cts.Cancel();

        // wait until task is done
        await requestHoverTextTask.ContinueWith(t => { },
            new CancellationToken(),
            TaskContinuationOptions.OnlyOnCanceled,
            TaskScheduler.Default);

        sentMessages
            .Select(m => m.Header.MessageType)
            .Should()
            .NotContain(JupyterMessageContentTypes.InterruptRequest);

        requestHoverTextTask
            .IsCanceled
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task can_handle_hover_text_not_found()
    {
        var code = "test";
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new InspectReply(StatusValues.Ok, false), Message.Create(new InspectRequest(code, 1, 0))),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var command = new RequestHoverText(code, SourceUtilities.GetPositionFromCursorOffset(code, 1));
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .OfType<HoverTextProduced>()
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task can_fail_on_hover_text_status_error()
    {
        var code = "test";
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new InspectReply(StatusValues.Error,
                                                     true,
                                                     new Dictionary<string, object>{ { "text/plain", "doc-comment"} } ),
                Message.Create(new InspectRequest(code, 1, 0))),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var command = new RequestHoverText(code, SourceUtilities.GetPositionFromCursorOffset(code, 1));
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .Contain(e => e is CommandFailed);

        events
            .OfType<HoverTextProduced>()
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task hover_text_unrecognized_text_formatting_is_removed()
    {
        var ansiEscapedText = 
            "\u001b[30mBlack,\u001b[31mRed,\u001b[32mGreen,\u001b[33mYellow,\u001b[34mBlue,\u001b[35mMagenta,\u001b[36mCyan,\u001b[37mWhite,\u001b[0mReset," +
            "\u001b[30;1mBright Black,\u001b[31;1mBright Red,\u001b[32;1mBright Green,\u001b[33;1mBright Yellow,\u001b[34;1mBright Blue,\u001b[35;1mBright Magenta,\u001b[36;1mBright Cyan,\u001b[37;1mBright White," +
            "\u001b[1mBold,\u001b[4mUnderline,\u001b[7mReversed," +
            "\u001b[1mm\u001b[3009567mi\u001b[30m\u001B[1mc";
        var code = "test";
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new InspectReply(StatusValues.Ok,
                                                     true,
                                                     new Dictionary<string, object>{ 
                                                         { "text/plain", ansiEscapedText} } ),
                Message.Create(new InspectRequest(code, 1, 0))),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var command = new RequestHoverText(code, SourceUtilities.GetPositionFromCursorOffset(code, 1));
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .Should()
            .ContainSingle<HoverTextProduced>()
            .Which
            .Content
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
            .Which
            .Value
            .Should()
            .Be("Black,Red,Green,Yellow,Blue,Magenta,Cyan,White,Reset,"
            + "Bright Black,Bright Red,Bright Green,Bright Yellow,Bright Blue,Bright Magenta,Bright Cyan,Bright White,"
            + "Bold,Underline,Reversed,"
            + "mic");
    }

    [Fact]
    public async Task can_cancel_signature_help_without_kernel_interrupt()
    {
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new InterruptReply(), Message.Create(new InterruptRequest())),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var waitForCommandReceieved = options.MessageTracker.SentMessages
                    .TakeUntil(m => m.Header.MessageType == JupyterMessageContentTypes.InspectRequest)
                    .ToTask();

        var cts = new CancellationTokenSource();
        var request = new RequestSignatureHelp("test", new LinePosition(0, 1));
        var requestSigHelpTask = kernel.SendAsync(request, cts.Token);

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        await waitForCommandReceieved;
        cts.Cancel();

        // wait until task is done
        await requestSigHelpTask.ContinueWith(t => { },
            new CancellationToken(),
            TaskContinuationOptions.OnlyOnCanceled,
            TaskScheduler.Default);

        sentMessages
            .Select(m => m.Header.MessageType)
            .Should()
            .NotContain(JupyterMessageContentTypes.InterruptRequest);

        requestSigHelpTask
            .IsCanceled
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task can_handle_signature_help_not_found()
    {
        var code = "test";
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new InspectReply(StatusValues.Ok, false), Message.Create(new InspectRequest(code, 1, 0))),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var command = new RequestSignatureHelp(code, SourceUtilities.GetPositionFromCursorOffset(code, 1));
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .OfType<SignatureHelpProduced>()
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task can_fail_on_signature_help_status_error()
    {
        var code = "test";
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new InspectReply(StatusValues.Error,
                                                     true,
                                                     new Dictionary<string, object>{ { "text/plain", "doc-comment"} } ),
                Message.Create(new InspectRequest(code, 1, 0))),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var command = new RequestSignatureHelp(code, SourceUtilities.GetPositionFromCursorOffset(code, 1));
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .Contain(e => e is CommandFailed);

        events
            .OfType<SignatureHelpProduced>()
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task signature_help_unrecognized_text_formatting_is_removed()
    {
        var ansiEscapedText =
            "\u001b[30mBlack,\u001b[31mRed,\u001b[32mGreen,\u001b[33mYellow,\u001b[34mBlue,\u001b[35mMagenta,\u001b[36mCyan,\u001b[37mWhite,\u001b[0mReset," +
            "\u001b[30;1mBright Black,\u001b[31;1mBright Red,\u001b[32;1mBright Green,\u001b[33;1mBright Yellow,\u001b[34;1mBright Blue,\u001b[35;1mBright Magenta,\u001b[36;1mBright Cyan,\u001b[37;1mBright White," +
            "\u001b[1mBold,\u001b[4mUnderline,\u001b[7mReversed," +
            "\u001b[1mm\u001b[3009567mi\u001b[30m\u001B[1mc";
        var code = "test";
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new InspectReply(StatusValues.Ok,
                                                     true,
                                                     new Dictionary<string, object>{
                                                         { "text/plain", ansiEscapedText} } ),
                Message.Create(new InspectRequest(code, 1, 0))),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var command = new RequestSignatureHelp(code, SourceUtilities.GetPositionFromCursorOffset(code, 1));
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .Should()
            .ContainSingle<SignatureHelpProduced>()
            .Which
            .Signatures
            .Select(s => s.Documentation)
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
            .Which
            .Value
            .Should()
            .Be("Black,Red,Green,Yellow,Blue,Magenta,Cyan,White,Reset,"
            + "Bright Black,Bright Red,Bright Green,Bright Yellow,Bright Blue,Bright Magenta,Bright Cyan,Bright White,"
            + "Bold,Underline,Reversed,"
            + "mic");
    }
    
    [Fact]
    public async Task can_cancel_completions_without_kernel_interrupt()
    {
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new InterruptReply(), Message.Create(new InterruptRequest())),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var waitForCommandReceieved = options.MessageTracker.SentMessages
                    .TakeUntil(m => m.Header.MessageType == JupyterMessageContentTypes.CompleteRequest)
                    .ToTask();

        var cts = new CancellationTokenSource();
        var request = new RequestCompletions("test", new LinePosition(0, 1));
        var requestCompletionsTask = kernel.SendAsync(request, cts.Token);

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        await waitForCommandReceieved;
        cts.Cancel();

        await requestCompletionsTask.ContinueWith(t => { },
            new CancellationToken(),
            TaskContinuationOptions.OnlyOnCanceled,
            TaskScheduler.Default);

        sentMessages
            .Select(m => m.Header.MessageType)
            .Should()
            .NotContain(JupyterMessageContentTypes.InterruptRequest);

        requestCompletionsTask
            .IsCanceled
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task can_fail_on_completions_status_error()
    {
        var code = "test";
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new CompleteReply(0, 1, new[] {"test1", "test2"}, null, StatusValues.Error),
                Message.Create(new CompleteRequest(code, 1))),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var command = new RequestCompletions(code, SourceUtilities.GetPositionFromCursorOffset(code, 1));
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .Contain(e => e is CommandFailed);

        events
            .OfType<CompletionsProduced>()
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task can_translate_completion_item_metadata_for_completions_produce()
    {
        var code = "test";
        var metadata = new Dictionary<string, IReadOnlyList<CompletionResultMetadata>>
        {
            {
                CompletionResultMetadata.Experimental, new[]
                {
                    new CompletionResultMetadata(0, 1, "test1-test", "function", "TEST1TEST"),
                    new CompletionResultMetadata(0, 1, "test2-test", "class", null)
                }
            }
        };
        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
                Message.CreateReply(new CompleteReply(0, 1, new[] {"test1", "test2"}, metadata, StatusValues.Ok),
                Message.Create(new CompleteRequest(code, 1))),
        }));

        var kernel = await CreateJupyterKernelAsync(options);

        var command = new RequestCompletions(code, SourceUtilities.GetPositionFromCursorOffset(code, 1));
        var result = await kernel.SendAsync(command);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[] {
                        new CompletionItem("TEST1TEST", "Method", "test1-test", "test1-test", "test1-test"),
                        new CompletionItem("test2-test", "Class", "test2-test", "test2-test", "test2-test")
                    });
    }
}
