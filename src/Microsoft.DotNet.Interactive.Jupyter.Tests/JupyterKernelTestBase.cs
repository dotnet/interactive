// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public abstract class JupyterKernelTestBase : IDisposable
{
    protected CompositeDisposable _disposables = new();

    // to re-record the tests for simulated playback with JuptyerTestData, set this to true
    protected const bool RECORD_FOR_PLAYBACK = false;
    protected const string PythonKernelName = "python3";
    protected const string RKernelName = "ir";

    public virtual void Dispose()
    {
        _disposables.Dispose();
    }

    protected CompositeKernel CreateCompositeKernelAsync(params IJupyterKernelConnectionOptions[] optionsList)
    {
        Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);

        var csharpKernel = new CSharpKernel()
                                .UseKernelHelpers()
                                .UseValueSharing();

        var kernel = new CompositeKernel { csharpKernel };
        kernel.DefaultKernelName = csharpKernel.Name;

        var jupyterKernelCommand = new ConnectJupyterKernelDirective();

        foreach (var options in optionsList) 
        {
            jupyterKernelCommand.AddConnectionOptions(options);
        }

        kernel.AddConnectDirective(jupyterKernelCommand);
        _disposables.Add(kernel);
        return kernel;
    }

    protected static List<Message> GenerateReplies(IReadOnlyCollection<Message> messages = null, string languageName = "name")
    {
        var replies = new List<Message>
        {
            // always sent as a first request
            Message.CreateReply(
                new KernelInfoReply("protocolVersion", "implementation", null,
                    new LanguageInfo(languageName, "version", "mimeType", "fileExt")),
                    Message.Create(new KernelInfoRequest()))
        };

        if (messages is not null)
        {
            // replies from the kernel start and end with status messages
            foreach (var m in messages)
            {
                replies.Add(Message.Create(new Status(StatusValues.Busy), m.ParentHeader));
                replies.Add(m);
                replies.Add(Message.Create(new Status(StatusValues.Idle), m.ParentHeader));
            }
        }

        return replies;
    }

    protected async Task<Kernel> CreateJupyterKernelAsync(TestJupyterConnectionOptions options, string kernelSpecName = null, string connectionString = null)
    {
        var kernel = CreateCompositeKernelAsync(options);

        var result = await kernel.SubmitCodeAsync($"#!connect jupyter --kernel-name testKernel --kernel-spec {kernelSpecName ?? "testKernelSpec"} {connectionString}");

        result.Events
            .Should()
            .NotContainErrors();

        return kernel.FindKernelByName("testKernel");
    }
}