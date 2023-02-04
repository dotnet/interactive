// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Assent;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.Interactive.Jupyter.Connection;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class JupyterKernelTests
{
    private readonly Configuration _configuration;
    private const bool SkipConnectionTests = true;
    private const string SkipReason = SkipConnectionTests ? "Setup not available" : null;
    
    public JupyterKernelTests()
    {
        _configuration = new Configuration()
            .UsingExtension("json");

        _configuration = _configuration.SetInteractive(Debugger.IsAttached);
    }

    private static CompositeKernel CreateKernelAsync(IJupyterKernelConnectionOptions options)
    {
        Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);
        var csharpKernel = new CSharpKernel();

        var kernel = new CompositeKernel
        {
            csharpKernel,
        };

        kernel.DefaultKernelName = csharpKernel.Name;

        var jupyterKernelCommand = new ConnectJupyterKernelCommand();

        kernel.AddKernelConnector(jupyterKernelCommand.AddConnectionOptions(options));
        return kernel;
    }

    [Theory]
    [InlineData(typeof(JupyterHttpKernelConnectionOptions), "python3", Skip = SkipReason)]
    [InlineData(typeof(JupyterHttpKernelConnectionOptions), "ir", Skip = SkipReason)]
    [InlineData(typeof(JupyterLocalKernelConnectionOptions), "python3", Skip = SkipReason)]
    [InlineData(typeof(JupyterLocalKernelConnectionOptions), "ir", Skip = SkipReason)]
    public async Task can_connect_to_jupyter_kernel(Type connectionOptionsToTest, string kernelSpecToTest)
    {
        var options = JupyterKernelTestHelper.GetConnectionOptions(connectionOptionsToTest);

        var kernel = CreateKernelAsync(options);

        var result = await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecToTest} {options.TestConnectionString}");

        result.KernelEvents
            .ToSubscribedList()
            .Should()
            .NotContainErrors();

        // should check on the kernel
        options.MessageTracker.SentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.KernelInfoRequest);

        // should send the comm message for setting up variable sharing channel
        options.MessageTracker.SentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.ExecuteRequest);

        options.MessageTracker.SentMessages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.CommOpen);
    }
}
