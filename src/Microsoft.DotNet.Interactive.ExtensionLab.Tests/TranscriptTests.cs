// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests;

[TestClass]
public class TranscriptTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    [TestMethod]
    public async Task transcript_extension_writes_all_received_commands_to_the_specified_file()
    {
        using var kernel = new CompositeKernel
        {
            new FSharpKernel()
        };

        await RecordTranscriptExtension.LoadAsync(kernel);

        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            Guid.NewGuid().ToString());

        _disposables.Add(Disposable.Create(() => File.Delete(filePath)));

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync($"#!record --output {filePath}");
        await kernel.SubmitCodeAsync("12345");

        await Task.Delay(500);

        var lines = await File.ReadAllLinesAsync(filePath);

        lines.Should().Contain(l => l.Contains("12345"));
    }

    public void Dispose() => _disposables.Dispose();
}