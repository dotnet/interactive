// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class SendEditableCodeTests
{
    [Fact]
    public async Task It_infers_InsertAtPosition_from_ambient_SubmitCode_cellIndex_parameter()
    {
        SendEditableCode receivedSendEditableCode = null;

        using var kernel = new CompositeKernel
        {
            new CSharpKernel()
        };
        kernel.DefaultKernelName = "csharp";
        kernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            receivedSendEditableCode = command;
            return Task.CompletedTask;
        });

        var submitCode = new SubmitCode(
            """
            using Microsoft.DotNet.Interactive;
            using Microsoft.DotNet.Interactive.Commands;

            await Kernel.Root.SendAsync(new SendEditableCode("csharp", "// new cell contents"));
            """);
        var cellIndex = 123;
        submitCode.Parameters.Add("cellIndex", cellIndex.ToString());

        var result = await kernel.SendAsync(submitCode);

        result.Events.Should().NotContainErrors();

        receivedSendEditableCode.Should().NotBeNull();

        receivedSendEditableCode.InsertAtPosition.Should().Be(cellIndex + 1);
    }
}