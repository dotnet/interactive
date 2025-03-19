// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
public class HtmlKernelTests
{
    [TestMethod]
    public async Task html_emits_string_as_content_within_a_script_element()
    {
        using var kernel = new CompositeKernel
        {
            new HtmlKernel()
        };

        var html = "<b>hello!</b>";

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SendAsync(new SubmitCode($"#!html\n\n{html}"));

        var formatted =
            events
                .OfType<DisplayedValueProduced>()
                .SelectMany(v => v.FormattedValues)
                .ToArray();

        formatted
            .Should()
            .ContainSingle(v => v.MimeType == "text/html")
            .Which
            .Value
            .Trim()
            .Should()
            .Be(html);
    }
}