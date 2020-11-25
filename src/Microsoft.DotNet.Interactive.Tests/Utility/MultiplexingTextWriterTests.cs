// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FSharp.Compiler.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.FSharp.Core;
using Xunit;
using static System.Environment;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    public class MultiplexingTextWriterTests
    {
        [Fact]
        public async Task It_writes_parallel_console_writes_to_separate_buffers()
        {
            var writer = new MultiplexingTextWriter();

            var barrier = new Barrier(10);

            var outputs = await Task.WhenAll(Enumerable.Range(1, 10).Select(i =>
            {
                return Task.Run(async () =>
                {
                    using var _ = writer.InitializeForCurrentAsyncContext();

                    barrier.SignalAndWait();

                    writer.Write($"Hello from {i}");
                    writer.Write('\n');

                    await Task.Yield();

                    barrier.SignalAndWait();

                    await writer.WriteLineAsync($"Goodbye from {i}");

                    barrier.SignalAndWait();

                    return (i, text: writer.ToString());
                });
            }));

            foreach (var output in outputs)
            {
                output.text.Should().Be($"Hello from {output.i}\nGoodbye from {output.i}{NewLine}");
            }
        }

        [Fact]
        public void Initialization_per_context_is_idempotent()
        {
            var writer = new MultiplexingTextWriter(() => new TestWriter());
            using var outer = writer.InitializeForCurrentAsyncContext();
            writer.Write("hi!");
            writer.Writers.Count().Should().Be(1);

            using var inner = writer.InitializeForCurrentAsyncContext();

            writer.Writers
                  .Should()
                  .ContainSingle()
                  .Which
                  .As<TestWriter>()
                  .Disposed
                  .Should()
                  .BeFalse();
        }

        [Fact]
        public void Disposal_of_outer_context_disposes_writer()
        {
            var writer = new MultiplexingTextWriter(() => new TestWriter());
            using var outer = writer.InitializeForCurrentAsyncContext();
            writer.Write("outer");
            var inner = writer.InitializeForCurrentAsyncContext();
            writer.Write(inner);

            var testWriter = writer.Writers.OfType<TestWriter>().Single();

            outer.Dispose();

            writer.Writers.Count().Should().Be(0);
        }

        [Fact]
        public void Disposal_of_inner_contexts_doe_not_dispose_writer()
        {
            var writer = new MultiplexingTextWriter();
            using var outer = writer.InitializeForCurrentAsyncContext();
            writer.Write("outer");
            var inner = writer.InitializeForCurrentAsyncContext();
            writer.Write(inner);
            
            inner.Dispose();

            writer.Writers.Count().Should().Be(1);
        }

        [Fact]
        public async Task EXPERIMENT_async_context_is_available_in_csharp_scripting()
        {
            var originalWriter = Console.Out;

            try
            {
                var multiWriter = new MultiplexingTextWriter();
                using var _ = multiWriter.InitializeForCurrentAsyncContext();

                Console.SetOut(multiWriter);

                // await kernel.SubmitCodeAsync("Console.WriteLine(\"hello!\");");
                var result = await CSharpScript.RunAsync("System.Console.WriteLine(\"hello!\");");

                result.Exception.Should().BeNull();

                multiWriter.ToString().Should().Be($"hello!{NewLine}");
            }
            finally
            {
                Console.SetOut(originalWriter);
            }
        }

        [Fact]
        public void EXPERIMENT_async_context_is_available_in_fsharp_scripting()
        {
            var originalWriter = Console.Out;

            try
            {
                var multiWriter = new MultiplexingTextWriter();
                multiWriter.InitializeForCurrentAsyncContext();

                Console.SetOut(multiWriter);

                var fSharpScript = new FSharpScript(
                    new FSharpOption<string[]>(new[] { "/langversion:preview", "/usesdkrefs-" }),
                    new FSharpOption<bool>(true),
                    new FSharpOption<LangVersion>(LangVersion.Preview));

                var result = fSharpScript.Eval(
                    "System.Console.WriteLine \"hello!\"",
                    new FSharpOption<CancellationToken>(CancellationToken.None));

                var ex = result.Item2;
                ex.Should().BeEmpty();

                multiWriter.ToString().Should().Be($"hello!{NewLine}");
            }
            finally
            {
                Console.SetOut(originalWriter);
            }
        }

        private class TestWriter : TextWriter
        {
            public override Encoding Encoding { get; } = new UnicodeEncoding();

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }

            public bool Disposed { get; private set; }
        }
    }
}