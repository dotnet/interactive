// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Recipes;
using Xunit;
using Xunit.Abstractions;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class CompleteRequestHandlerTests : JupyterRequestHandlerTestBase
    {
        public CompleteRequestHandlerTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task send_completeReply_on_CompleteRequest()
        {
            var scheduler = CreateScheduler();
            var request = ZeroMQMessage.Create(new CompleteRequest("System.Console.", 15));
            var context = new JupyterRequestContext(JupyterMessageSender, request);

            await scheduler.Schedule(context);
            
            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages
                .Should()
                .ContainSingle(r => r is CompleteReply);
        }
    }
}