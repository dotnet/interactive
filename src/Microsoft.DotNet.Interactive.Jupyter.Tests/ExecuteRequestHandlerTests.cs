﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests;
using Newtonsoft.Json.Linq;
using Pocket;
using Recipes;
using Xunit;
using Xunit.Abstractions;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
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
                .BeEquivalentTo(Environment.NewLine + "(1,13): error CS1002: ; expected" + Environment.NewLine + Environment.NewLine);
        }

        [Theory]
        [InlineData(Language.CSharp, "(1,4): error CS1733: Expected expression")]
        [InlineData(Language.FSharp, "input.fsx (1,4)-(1,4) parse error Unexpected end of input in expression")]
        public async Task shows_diagnostics_on_erroneous_input(Language language, string expected)
        {
            var scheduler = CreateScheduler();
            SetKernelLanguage(language);
            var request = ZeroMQMessage.Create(new ExecuteRequest("1+!"));
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.PubSubMessages.Should()
                .ContainSingle<Stream>()
                .Which
                .Text
                .Should()
                .Contain(expected);
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
                                .ContainSingle<DisplayData>(r => r.Data["application/json"] is JToken token && token.Type == JTokenType.Integer);
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
            
            command.Properties["publish-internal-events"] = true;

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

            JupyterMessageSender.PubSubMessages.Should().Contain(r => r is Stream && r.As<Stream>().Name == Stream.StandardOutput);
        }

        [Fact]
        public async Task sends_Stream_message_on_StandardErrorValueProduced()
        {
            var scheduler = CreateScheduler();
            var request = ZeroMQMessage.Create(new ExecuteRequest("Console.Error.WriteLine(2+2);"));
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages.Should().Contain(r => r is Stream && r.As<Stream>().Name == Stream.StandardError);
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
                                .ContainSingle(d => d.Key.Equals("text/html") && d.Value.Equals("<div class=\"dni-plaintext\">4</div>"));
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
                                .ContainSingle(d => d.Key.Equals("text/html") && d.Value.Equals("<div class=\"dni-plaintext\">123</div>"));
        }

        [Theory]
        [InlineData("input()", "", "input-value")]
        [InlineData("input(\"User: \")", "User: ", "user name")]
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
        [InlineData("Read-Host", "", "input-value")]
        [InlineData("Read-Host -Prompt User", "User: ", "user name")]
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
            var request = ZeroMQMessage.Create(new ExecuteRequest("password(\"Password:\")"));
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            log.ToString().Should().NotContain("secret");
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
                .Contain(dp => dp.Data["text/html"] as string == $"<div class=\"dni-plaintext\">{typeof(PasswordString).FullName}</div>");
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
                .OfType<Stream>()
                .Should()
                .Contain(s => s.Name == Stream.StandardOutput && s.Text == $"System.Security.SecureString{Environment.NewLine}");
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
                  .StartWith("System.NotSupportedException: Input request is not supported");
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
                .StartWith("System.NotSupportedException: Password request is not supported");
        }
    }
}
