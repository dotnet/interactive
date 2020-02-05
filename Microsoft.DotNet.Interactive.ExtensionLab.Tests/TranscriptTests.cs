// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests;
using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class TranscriptTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        [Fact]
        public async Task transcript_extension_writes_all_received_commands_to_the_specified_file()
        {
            var kernel = new CompositeKernel
            {
                new FSharpKernel()
            };

            await new RecordTranscriptExtension().OnLoadAsync(kernel);

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                Guid.NewGuid().ToString());

            _disposables.Add(Disposable.Create(() => File.Delete(filePath)));

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"#!record --path {filePath}");
            await kernel.SubmitCodeAsync("12345");

            var lines = File.ReadAllLines(filePath);

            lines.First().Should().Contain("12345");
        }

        public void Dispose() => _disposables.Dispose();
    }
}