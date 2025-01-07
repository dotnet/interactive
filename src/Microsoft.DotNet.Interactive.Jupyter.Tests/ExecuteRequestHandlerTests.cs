// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests;
using Pocket;
using Recipes;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.DotNet.Interactive.Formatting.Tests.Tags;
using static Microsoft.DotNet.Interactive.Jupyter.Tests.RecordingJupyterMessageSender;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

[Trait("Category", "Skip")]
public class ExecuteRequestHandlerTests : JupyterRequestHandlerTestBase
{
    public ExecuteRequestHandlerTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task sends_ExecuteInput_when_ExecuteRequest_is_handled()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("var a =12;"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(5.Seconds());

        JupyterMessageSender.PubSubMessages.Should()
            .ContainItemsAssignableTo<ExecuteInput>();

        JupyterMessageSender.PubSubMessages.OfType<ExecuteInput>().Should().Contain(r => r.Code == "var a =12;");
    }

    [Fact]
    public async Task sends_ExecuteReply_message_on_when_code_submission_is_handled()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("var a =12;"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(5.Seconds());

        JupyterMessageSender.ReplyMessages
            .Should()
            .ContainItemsAssignableTo<ExecuteReplyOk>();
    }

    [Fact]
    public async Task sends_ExecuteReply_with_error_message_on_when_code_submission_contains_errors()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("asdes"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(5.Seconds());

        JupyterMessageSender.ReplyMessages.Should().ContainItemsAssignableTo<ExecuteReplyError>();
        JupyterMessageSender.PubSubMessages.Should().Contain(e => e is Error);
    }

    [Fact]
    public async Task Shows_informative_exception_information()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(
            new ExecuteRequest(@"
void f()
{
    try
    {
        throw new Exception(""the-inner-exception"");
    }
    catch(Exception e)
    {
        throw new DataMisalignedException(""the-outer-exception"", e);
    }
    
}

f();"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);

        await scheduler.Schedule(context);

        await context.Done().Timeout(5.Seconds());

        var traceback = JupyterMessageSender
            .PubSubMessages
            .Should()
            .ContainSingle(e => e is Error)
            .Which
            .As<Error>()
            .Traceback;

        var errorMessage = string.Join("\n", traceback);

        errorMessage
            .Should()
            .StartWith("System.DataMisalignedException: the-outer-exception")
            .And
            .Contain("---> System.Exception: the-inner-exception");
    }

    [Fact]
    public async Task does_not_expose_stacktrace_when_code_submission_contains_errors()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("asdes asdasd"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(5.Seconds());

        JupyterMessageSender.PubSubMessages.Should()
            .ContainSingle<Stream>()
            .Which
            .Text
            .Should()
            .ContainAll("(1,13): error CS1002:", ";");
    }

    [Theory]
    [InlineData(Language.CSharp, "(1,4): error CS1733:")]
    [InlineData(Language.FSharp, "input.fsx (1,2)-(1,4) parse error Unexpected token '+!' or incomplete expression")]
    public async Task shows_diagnostics_on_erroneous_input(Language language, string expected)
    {
        var scheduler = CreateScheduler();
        SetKernelLanguage(language);
        var request = ZeroMQMessage.Create(new ExecuteRequest("1+!"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(5.Seconds());

        JupyterMessageSender.PubSubMessages.OfType<Protocol.Stream>()
            .Should()
            .ContainSingle(error => error.Text.Contains(expected));
    }

    [Fact]
    public async Task sends_DisplayData_message_on_ValueProduced()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("display(2+2);"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.PubSubMessages.Should().Contain(r => r is DisplayData);
    }

    [Fact]
    public async Task sends_DisplayData_message_with_json_when_json_mimetype_is_requested()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest(@"display(2+2,""application/json"");"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.PubSubMessages
            .Should()
            .ContainSingle<DisplayData>(r => r.Data["application/json"] is JsonElement element && element.ValueKind == JsonValueKind.Number);
    }
        
    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task does_not_send_ExecuteResult_message_when_evaluating_display_value(Language language)
    {
        var scheduler = CreateScheduler();
        SetKernelLanguage(language);
        var request = ZeroMQMessage.Create(new ExecuteRequest("display(2+2)"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.PubSubMessages.Should().NotContain(r => r is ExecuteResult);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task deferred_command_can_produce_events(Language language)
    {
        var scheduler = CreateScheduler();
        SetKernelLanguage(language);
        var command = new SubmitCode(@"#!html
<p>hello!</p>", Kernel.Name);
        
        DeferCommand(command);
        var request = ZeroMQMessage.Create(new ExecuteRequest("display(2+2)"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.PubSubMessages
            .OfType<DisplayData>()
            .Should()
            .Contain(dp => dp.Data["text/html"] .ToString().Trim() == "<p>hello!</p>");
    }

    [Fact]
    public async Task sends_Stream_message_on_StandardOutputValueProduced()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("Console.WriteLine(2+2);"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.PubSubMessages.Should().Contain(r => r is Protocol.Stream && r.As<Protocol.Stream>().Name == Protocol.Stream.StandardOutput);
    }

    [Fact]
    public async Task sends_Stream_message_on_StandardErrorValueProduced()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("Console.Error.WriteLine(2+2);"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.PubSubMessages.Should().Contain(r => r is Protocol.Stream && r.As<Protocol.Stream>().Name == Protocol.Stream.StandardError);
    }

    [Fact]
    public async Task sends_ExecuteReply_message_on_ReturnValueProduced()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("2+2"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        using var _ = new AssertionScope();

        JupyterMessageSender.PubSubMessages
            .Should()
            .ContainSingle<ExecuteResult>()
            .Which
            .Data
            .Should()
            .ContainSingle(d => d.Key.Equals("text/html") && 
                                d.Value.As<string>().RemoveStyleElement().Equals($"{PlainTextBegin}4{PlainTextEnd}"));
    }

    [Fact]
    public async Task sends_ExecuteReply_message_when_submission_contains_only_a_directive()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("#!csharp"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(5.Seconds());

        JupyterMessageSender.ReplyMessages
            .Should()
            .ContainSingle<ExecuteReplyOk>();
    }

    [Fact]
    public async Task sends_ExecuteReply_message_when_submission_contains_a_language_directive_and_trailing_expression()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("#!fsharp\n123"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(5.Seconds());

        using var _ = new AssertionScope();

        JupyterMessageSender.ReplyMessages
            .Should()
            .ContainSingle<ExecuteReplyOk>();
        JupyterMessageSender.PubSubMessages
            .Should()
            .ContainSingle<ExecuteResult>()
            .Which
            .Data
            .Should()
            .ContainSingle(d => d.Key.Equals("text/html") && 
                                d.Value.As<string>().RemoveStyleElement().Equals($"{PlainTextBegin}123{PlainTextEnd}"));
    }

    [Theory]
    [InlineData("input()", "", InputForUnspecifiedPrompt)]
    [InlineData($"input(\"{InputPromptForUser}\")", InputPromptForUser, InputForUser)]
    [InlineData($"await Microsoft.DotNet.Interactive.Kernel.GetInputAsync(\"{InputPromptForUser}\")", InputPromptForUser, InputForUser)]
    public async Task sends_InputRequest_message_when_submission_requests_user_input_in_csharp(string code, string prompt, string expectedDisplayValue)
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest(code));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.RequestMessages.Should().Contain(r => r.Prompt == prompt && r.Password == false);
        JupyterMessageSender.PubSubMessages
            .OfType<ExecuteResult>()
            .Should()
            .Contain(dp => dp.Data["text/plain"] as string == expectedDisplayValue);
    }

    [Theory]
    [InlineData("Read-Host", "", InputForUnspecifiedPrompt)]
    [InlineData("Read-Host -Prompt User", InputPromptForUser, InputForUser)]
    public async Task sends_InputRequest_message_when_submission_requests_user_input_in_powershell(string code, string prompt, string expectedDisplayValue)
    {
        SetKernelLanguage(Language.PowerShell);

        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest(code));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.RequestMessages.Should().Contain(r => r.Prompt == prompt && r.Password == false);
        JupyterMessageSender.PubSubMessages
            .OfType<Stream>()
            .Should()
            .Contain(s => s.Name == Stream.StandardOutput && s.Text == (expectedDisplayValue + Environment.NewLine));
    }

    [Fact]
    public async Task password_input_should_not_appear_in_diagnostic_logs()
    {
        var log = new System.Text.StringBuilder();
        using var _ = LogEvents.Subscribe(e => log.Append(e.ToLogString()));

        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest($"password(\"{InputPromptForPassword}\")"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        log.ToString().Should().NotContain(InputForPassword);
    }

    [Theory]
    [InlineData("password()", "")]
    [InlineData("password(\"Type your password:\")", "Type your password:")]
    public async Task sends_InputRequest_message_when_submission_requests_user_password_in_csharp(string code, string prompt)
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest(code));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.RequestMessages.Should().Contain(r => r.Prompt == prompt && r.Password);

        JupyterMessageSender.PubSubMessages
                            .OfType<ExecuteResult>()
                            .Should()
                            .Contain(dp => (dp.Data["text/html"] as string).Contains("<code>************</code>"),
                                     because: $" password function returns the {typeof(PasswordString)} instance");
    }

    [Theory]
    [InlineData("Read-Host -AsSecureString", "")]
    [InlineData("Read-Host -Prompt 'Type your password' -AsSecureString", "Type your password: ")]
    public async Task sends_InputRequest_message_when_submission_requests_user_password_in_powershell(string code, string prompt)
    {
        SetKernelLanguage(Language.PowerShell);

        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest(code));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        await scheduler.Schedule(context);

        await context.Done().Timeout(20.Seconds());

        JupyterMessageSender.RequestMessages.Should().Contain(r => r.Prompt == prompt && r.Password == true);
        JupyterMessageSender.PubSubMessages
            .OfType<Protocol.Stream>()
            .Should()
            .Contain(s => s.Name == Protocol.Stream.StandardOutput && s.Text == $"System.Security.SecureString{Environment.NewLine}");
    }

    [Fact]
    public async Task Shows_not_supported_exception_when_stdin_not_allowed_and_input_is_requested()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("input()", allowStdin: false));
        var context = new JupyterRequestContext(JupyterMessageSender, request);

        await scheduler.Schedule(context);
        await context.Done().Timeout(5.Seconds());

        var traceback = JupyterMessageSender
            .PubSubMessages
            .Should()
            .ContainSingle(e => e is Error)
            .Which
            .As<Error>()
            .Traceback;

        var errorMessage = string.Join("\n", traceback);
        errorMessage
            .Should()
            .StartWith("System.NotSupportedException: Input prompt is not supported");
    }

    [Fact]
    public async Task Shows_not_supported_exception_when_stdin_not_allowed_and_password_is_requested()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new ExecuteRequest("password()", allowStdin: false));
        var context = new JupyterRequestContext(JupyterMessageSender, request);

        await scheduler.Schedule(context);
        await context.Done().Timeout(5.Seconds());

        var traceback = JupyterMessageSender
            .PubSubMessages
            .Should()
            .ContainSingle(e => e is Error)
            .Which
            .As<Error>()
            .Traceback;

        var errorMessage = string.Join("\n", traceback);
        errorMessage
            .Should()
            .StartWith("System.NotSupportedException: Password prompt is not supported.");
    }

    [Fact]
    public void cell_kernel_name_can_be_pulled_from_dotnet_metadata_when_present()
    {
        var metaData = new Dictionary<string, object>
        {
            // the value specified is `language`, but in reality this was the kernel name
            { "dotnet_interactive", new InputCellMetadata(language: "fsharp") }
        };
        var request = ZeroMQMessage.Create(new ExecuteRequest("1+1"), metaData: metaData);
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var kernelName = context.GetKernelName();
        kernelName
            .Should()
            .Be("fsharp");
    }

    [Fact]
    public void cell_kernel_name_can_be_pulled_from_polyglot_metadata_when_present()
    {
        var metaData = new Dictionary<string, object>
        {
            { "polyglot_notebook", new InputCellMetadata(kernelName: "fsharp") }
        };
        var request = ZeroMQMessage.Create(new ExecuteRequest("1+1"), metaData: metaData);
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var kernelName = context.GetKernelName();
        kernelName
            .Should()
            .Be("fsharp");
    }

    [Fact]
    public void cell_kernel_name_in_polyglot_metadata_overrides_dotnet_metadata()
    {
        var metaData = new Dictionary<string, object>
        {
            { "dotnet_interactive", new InputCellMetadata(language: "not-fsharp") },
            { "polyglot_notebook", new InputCellMetadata(kernelName: "fsharp") }
        };
        var request = ZeroMQMessage.Create(new ExecuteRequest("1+1"), metaData: metaData);
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var kernelName = context.GetKernelName();
        kernelName
            .Should()
            .Be("fsharp");
    }

    [Fact]
    public void cell_kernel_name_defaults_to_null_when_it_cant_be_found()
    {
        var request = ZeroMQMessage.Create(new ExecuteRequest("1+1"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var kernelName = context.GetKernelName();
        kernelName
            .Should()
            .BeNull();
    }
}