// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Mermaid;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests;

public class ExplainCodeExtensionTest
{
    [Fact]
    public async Task it_requires_mermaid_kernel_to_load()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel(),
        };
        

        var executeTask = () => ExplainCodeExtension.LoadAsync(kernel);
        await executeTask.Should().ThrowAsync<InvalidOperationException>();

    }

    [Fact]
    public async Task generates_interaction_diagram_for_top_level_statements()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel(),
            new MermaidKernel()
        };

        var events = kernel.KernelEvents.ToSubscribedList();
        var extension = new ExplainCodeExtension();
        await ExplainCodeExtension.LoadAsync(kernel);

        await kernel.SendAsync(new SubmitCode(@"
#!explain
using System;

var data = new[] { 1, 2, 3 };
for (var i = 0; i < data.Length; i++)
{
    Console.WriteLine(i.ToString());
}
", targetKernelName: "csharp"));

        events.Should().ContainSingle<DisplayedValueProduced>()
            .Which
            .Value.ToString()
            .Should()
            .Contain(@"sequenceDiagram
 loop
    CodeSubmission->>+i: invoke ToString
    i->>-CodeSubmission: return
    CodeSubmission->>+Console: invoke WriteLine
    Console->>-CodeSubmission: return
 end".Replace("\r\n", "\n"));
    }
}