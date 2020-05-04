// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Clockwise;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Recipes;
using Xunit;
using Xunit.Abstractions;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class IsCompleteRequestHandlerTests : JupyterRequestHandlerTestBase
    {
        public IsCompleteRequestHandlerTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task sends_isCompleteReply_with_complete_if_the_code_is_a_complete_submission()
        {
            var scheduler = CreateScheduler();
            var request = ZeroMQMessage
.Create(new IsCompleteRequest("var a = 12;"), null);
            var context = new JupyterRequestContext(JupyterMessageSender, request);

            await scheduler.Schedule(context);
            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages
                                .OfType<IsCompleteReply>()
                                .Should()
                                .ContainSingle(r => r.Status == "complete");
        }

        [Fact]
        public async Task sends_isCompleteReply_with_incomplete_and_indent_if_the_code_is_not_a_complete_submission()
        {
            var scheduler = CreateScheduler();
            var request = ZeroMQMessage
.Create(new IsCompleteRequest("var a = 12"), null);
            var context = new JupyterRequestContext(JupyterMessageSender, request);

            await scheduler.Schedule(context);
            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages.OfType<IsCompleteReply>().Should().ContainSingle(r => r.Status == "incomplete" && r.Indent == "*");
        }
    }
}