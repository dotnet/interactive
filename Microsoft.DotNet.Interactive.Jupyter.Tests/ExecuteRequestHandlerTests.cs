// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using FluentAssertions.Extensions;
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
    public class ExecuteRequestHandlerTests : JupyterRequestHandlerTestBase<ExecuteRequest>
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
                .ContainSingle(e => e is Error)
                .Which.As<Error>()
                .Traceback
                .Should()
                .BeEquivalentTo("(1,13): error CS1002: ; expected");
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
                .ContainSingle<DisplayData>(r => r.Data["application/json"] is JToken token && token.Type ==JTokenType.Integer);
        }

        [Theory]
        [InlineData(Language.CSharp, "display(new LaTeXString(@\"F(k) = \\int_{-\\infty}^{\\infty} f(x) e^{2\\pi i k} dx\"));", @"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx")]
        [InlineData(Language.FSharp, "display(new LaTeXString(@\"F(k) = \\int_{-\\infty}^{\\infty} f(x) e^{2\\pi i k} dx\"))", @"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx")]
        public async Task does_display_LaTeXString_values_on_ValueProduced(Language language, string code, string expectedDisplayValue)
        {
            var scheduler = CreateScheduler();
            SetKernelLanguage(language);
            var request = ZeroMQMessage.Create(new ExecuteRequest(code));
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages
                .OfType<DisplayData>()
                .Should()
                .Contain(dp=> dp.Data["text/latex"] as string == expectedDisplayValue);
        }

        [Theory]
        [InlineData(Language.CSharp, "new LaTeXString(@\"F(k) = \\int_{-\\infty}^{\\infty} f(x) e^{2\\pi i k} dx\")", @"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx")]
        [InlineData(Language.FSharp, "new LaTeXString(@\"F(k) = \\int_{-\\infty}^{\\infty} f(x) e^{2\\pi i k} dx\")", @"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx")]
        public async Task does_display_LaTeXString_values_on_ReturnValueProduced(Language language, string code, string expectedDisplayValue)
        {
            var scheduler = CreateScheduler();
            SetKernelLanguage(language);
            var request = ZeroMQMessage.Create(new ExecuteRequest(code));
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages
                .OfType<ExecuteResult>()
                .Should()

                .Contain(dp => dp.Data["text/latex"] as string == expectedDisplayValue);
        }

        [Theory]
        [InlineData(Language.CSharp, "display(new MathString(@\"F(k) = \\int_{-\\infty}^{\\infty} f(x) e^{2\\pi i k} dx\"));", @"$$F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx$$")]
        [InlineData(Language.FSharp, "display(new MathString(@\"F(k) = \\int_{-\\infty}^{\\infty} f(x) e^{2\\pi i k} dx\"))", @"$$F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx$$")]
        public async Task does_display_MathString_values_on_ValueProduced(Language language, string code, string expectedDisplayValue)
        {
            var scheduler = CreateScheduler();
            SetKernelLanguage(language);
            var request = ZeroMQMessage.Create(new ExecuteRequest(code));
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages
                .OfType<DisplayData>()
                .Should()

                .Contain(dp => dp.Data["text/latex"] as string == expectedDisplayValue);
        }

        [Theory]
        [InlineData(Language.CSharp, "new MathString(@\"F(k) = \\int_{-\\infty}^{\\infty} f(x) e^{2\\pi i k} dx\")", @"$$F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx$$")]
        [InlineData(Language.FSharp, "new MathString(@\"F(k) = \\int_{-\\infty}^{\\infty} f(x) e^{2\\pi i k} dx\")", @"$$F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx$$")]
        public async Task does_display_MathString_values_on_ReturnValueProduced(Language language, string code, string expectedDisplayValue)
        {
            var scheduler = CreateScheduler();
            SetKernelLanguage(language);
            var request = ZeroMQMessage.Create(new ExecuteRequest(code));
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages
                .OfType<ExecuteResult>()
                .Should()
                .Contain(dp => dp.Data["text/latex"] as string == expectedDisplayValue);
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

            JupyterMessageSender.PubSubMessages.Should().Contain(r => r is ExecuteResult);
        }

        [Fact]
        public async Task sends_ExecuteReply_message_when_submission_contains_only_a_directive()
        {
            var scheduler = CreateScheduler();
            var request = ZeroMQMessage.Create(new ExecuteRequest("#!csharp"));
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages.Should().ContainItemsAssignableTo<ExecuteReplyOk>();
        }

        [Theory]
        [InlineData("input()", "", "input-value")]
        [InlineData("input(\"User:\")", "User:", "user name")]
        public async Task sends_InputRequest_message_when_submission_requests_user_input(string code, string prompt, string expectedDisplayValue)
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
        public async Task sends_InputRequest_message_when_submission_requests_user_password(string code, string prompt)
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
                .Contain(dp => dp.Data["text/html"] as string == $"{typeof(PasswordString).FullName}");
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
