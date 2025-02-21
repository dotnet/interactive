// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Xunit;
using Xunit.Abstractions;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class IsCompleteRequestHandlerTests : JupyterRequestHandlerTestBase
{
    public IsCompleteRequestHandlerTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task sends_isCompleteReply_with_complete_if_the_code_is_a_complete_submission()
    {
        var scheduler = CreateScheduler();
        var request = ZeroMQMessage.Create(new IsCompleteRequest("var a = 12;"), null);
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
        var request = ZeroMQMessage.Create(new IsCompleteRequest("var a = 12"), null);
        var context = new JupyterRequestContext(JupyterMessageSender, request);

        await scheduler.Schedule(context);
        await context.Done().Timeout(5.Seconds());

        JupyterMessageSender.ReplyMessages.OfType<IsCompleteReply>().Should().ContainSingle(r => r.Status == "incomplete" && r.Indent == "*");
    }

    [Fact]
    public void cell_language_can_be_pulled_from_dotnet_interactive_metadata_when_present()
    {
        var metaData = new Dictionary<string, object>
        {
            // the value specified is `language`, but in reality this was the kernel name
            { "dotnet_interactive", new InputCellMetadata(language: "fsharp") }
        };
        var request = ZeroMQMessage.Create(new IsCompleteRequest("1+1"), metaData: metaData);
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var kernelName = context.GetKernelName();
        kernelName
            .Should()
            .Be("fsharp");
    }

    [Fact]
    public void cell_language_can_be_pulled_from_polyglot_notebook_metadata_when_present()
    {
        var metaData = new Dictionary<string, object>
        {
            { "polyglot_notebook", new InputCellMetadata(kernelName: "fsharp") }
        };
        var request = ZeroMQMessage.Create(new IsCompleteRequest("1+1"), metaData: metaData);
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var kernelName = context.GetKernelName();
        kernelName
            .Should()
            .Be("fsharp");
    }

    [Fact]
    public void cell_language_defaults_to_null_when_it_cant_be_found()
    {
        var request = ZeroMQMessage.Create(new IsCompleteRequest("1+1"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var language = context.GetKernelName();
        language
            .Should()
            .BeNull();
    }
}