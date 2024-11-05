// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.Interactive.Parsing;
using Pocket;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public abstract class DocumentFormatTestsBase : IDisposable
{
    private CompositeKernel _kernel;
    private readonly CompositeDisposable _disposables = new();

    protected DocumentFormatTestsBase()
    {
        DefaultKernelInfos = new KernelInfoCollection
        {
            new("csharp", "C#", new[] { "cs", "C#", "c#" }),
            new("fsharp", "F#", new[] { "fs", "F#", "f#" }),
            new("pwsh", "PowerShell", new[] { "powershell" }),
        };
        DefaultKernelInfos.DefaultKernelName = "csharp";
    }

    public KernelInfoCollection DefaultKernelInfos { get; }

    public DirectiveParseResult ParseDirectiveLine(string directiveLine) => SubmissionParser.ParseDirectiveLine(directiveLine);

    private SubmissionParser SubmissionParser
    {
        get
        {
            EnsureKernelIsInitialized();

            return _kernel.SubmissionParser;
        }
    }

    private void EnsureKernelIsInitialized()
    {
        _kernel = new CompositeKernel().UseImportMagicCommand();

        _disposables.Add(_kernel);
    }

    protected static string PathToCurrentSourceFile([CallerFilePath] string path = null)
    {
        return path;
    }

    public void Dispose() => _disposables.Dispose();
}