using FluentAssertions;
using Microsoft.CodeAnalysis.Differencing;
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

public partial class JupyterKernelTests
{
    private static List<Message> GenerateReplies(IReadOnlyCollection<Message> messages = null, string languageName = "name")
    {
        var replies = new List<Message>()
        {
            // always sent as a first request
            Message.CreateReply(
                new KernelInfoReply("protocolVersion", "implementation", null,
                    new LanguageInfo(languageName, "version", "mimeType", "fileExt")),
                    Message.Create(new KernelInfoRequest()))
        };

        if (messages is not null)
        {
            // replies from the kernel start and end with status messages
            foreach (var m in messages)
            {
                replies.Add(Message.Create(new Status(StatusValues.Busy), m.ParentHeader));
                replies.Add(m);
                replies.Add(Message.Create(new Status(StatusValues.Idle), m.ParentHeader));
            }
        }

        return replies;
    }

    private async Task<Kernel> CreateJupyterKernelAsync(TestJupyterConnectionOptions options, string kernelSpecName = null, string connectionString = null)
    {
        var kernel = CreateKernelAsync(options);

        var result = await kernel.SubmitCodeAsync($"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecName ?? "testKernelSpec"} {connectionString}");

        result.Events
            .Should()
            .NotContainErrors();

        return kernel.FindKernelByName("testKernel");
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
    public async Task can_cancel_submit_code_and_get_interrupt_request_to_kernel()
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
    public async Task can_fail_on_inspect_reply_status_error()
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
}
