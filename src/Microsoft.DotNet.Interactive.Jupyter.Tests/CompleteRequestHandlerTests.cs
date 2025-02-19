// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Recipes;

using Xunit;
using Xunit.Abstractions;

using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

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
    public void kernel_name_can_be_pulled_from_dotnet_interactive_metadata_when_present()
    {
        var metaData = new Dictionary<string, object>
        {
            // the value specified is `language`, but in reality this was the kernel name
            { "dotnet_interactive", new InputCellMetadata(language: "fsharp") }
        };
        var request = ZeroMQMessage.Create(new CompleteRequest("1+1"), metaData: metaData);
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var kernel = context.GetKernelName();
        kernel
            .Should()
            .Be("fsharp");
    }

    [Fact]
    public void kernel_name_can_be_pulled_from_polyglot_notebook_metadata_when_present()
    {
        var metaData = new Dictionary<string, object>
        {
            { "polyglot_notebook", new InputCellMetadata(kernelName: "fsharp") }
        };
        var request = ZeroMQMessage.Create(new CompleteRequest("1+1"), metaData: metaData);
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var kernel = context.GetKernelName();
        kernel
            .Should()
            .Be("fsharp");
    }

    [Fact]
    public void cell_language_defaults_to_null_when_it_cant_be_found()
    {
        var request = ZeroMQMessage.Create(new CompleteRequest("1+1"));
        var context = new JupyterRequestContext(JupyterMessageSender, request);
        var language = context.GetKernelName();
        language
            .Should()
            .BeNull();
    }
}