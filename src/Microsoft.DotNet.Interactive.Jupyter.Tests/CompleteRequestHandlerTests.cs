// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        [Fact]
        public void cell_language_can_be_pulled_from_metadata_when_present()
        {
            var metaData = new Dictionary<string, object>()
            {
                { "dotnet_interactive", JObject.Parse(JsonConvert.SerializeObject(new { language = "fsharp" })) }
            };
            var request = ZeroMQMessage.Create(new CompleteRequest("1+1"), metaData: metaData);
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            var language = context.GetLanguage();
            language
                .Should()
                .Be("fsharp");
        }

        [Fact]
        public void cell_language_defaults_to_null_when_it_cant_be_found()
        {
            var request = ZeroMQMessage.Create(new CompleteRequest("1+1"));
            var context = new JupyterRequestContext(JupyterMessageSender, request);
            var language = context.GetLanguage();
            language
                .Should()
                .BeNull();
        }
    }
}