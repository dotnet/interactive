using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public partial class JupyterKernelTests
{
    private static IReadOnlyCollection<Message> GenerateReplies(IReadOnlyCollection<Message> messages = null, string languageName = "name")
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

        await kernel.SubmitCodeAsync($"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecName ?? "testKernelSpec"} {connectionString}");

        return kernel.FindKernelByName("testKernel");
    }

    [Fact]
    public async Task submit_code_line_endings_are_normalized_to_LF()
    {
        string code = "\r\ntest\r\ncode\r\n";
        var request = Message.Create(new ExecuteRequest(code));

        var options = new TestJupyterConnectionOptions(GenerateReplies(new[] {
            Message.CreateReply(new ExecuteReplyOk(), request)
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
            .Be("\ntest\ncode\n");
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
}
