// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using static System.Environment;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

[TestClass]
public class MultiplexingTextWriterTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public MultiplexingTextWriterTests(TestContext output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    [TestMethod]
    public async Task It_writes_parallel_console_writes_to_separate_buffers()
    {
        await using var writer = new MultiplexingTextWriter("out");

        var barrier = new Barrier(10);

        var outputs = await Task.WhenAll(Enumerable.Range(1, 10).Select(i =>
        {
            return Task.Run(async () =>
            {
                using var _ = writer.EnsureInitializedForCurrentAsyncContext();

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

    [TestMethod]
    public void Initialization_per_context_is_idempotent()
    {
        using var writer = new MultiplexingTextWriter("out", () => new TestWriter());
        using var outer = writer.EnsureInitializedForCurrentAsyncContext();
        writer.Write("hi!");
        writer.Writers.Count().Should().Be(1);

        using var inner = writer.EnsureInitializedForCurrentAsyncContext();

        writer.Writers
            .Should()
            .ContainSingle()
            .Which
            .As<TestWriter>()
            .Disposed
            .Should()
            .BeFalse();
    }
        
    [TestMethod]
    [DynamicData(nameof(WriteOperations))]
    public void Write_operations_on_MultiplexingStringWriter_are_observable_and_produce_one_event_per_top_level_write_invocation(
        Action<TextWriter> write, 
        string expectedValue)
    {
        using var writer = new MultiplexingTextWriter("out");

        using var _ = writer.EnsureInitializedForCurrentAsyncContext();

        using var events = writer.GetObservable().ToSubscribedList();

        write(writer);

        events.Should()
            .ContainSingle()
            .Which
            .Should()
            .Be(expectedValue);
    }

    [TestMethod]
    public async Task Multiple_instances_can_initialized_in_one_async_context()
    {
        await using var writerOne = new MultiplexingTextWriter("one");
        await using var writerTwo = new MultiplexingTextWriter("two");

        writerOne.EnsureInitializedForCurrentAsyncContext();
        writerTwo.EnsureInitializedForCurrentAsyncContext();

        await writerOne.WriteAsync("one");
        await writerTwo.WriteAsync("two");

        writerOne.ToString().Should().Be("one");
        writerTwo.ToString().Should().Be("two");
    }

    [TestMethod]
    [DynamicData(nameof(WriteOperations))]
    public void Write_operations_on_ObservableStringWriter_are_observable_and_produce_one_event_per_top_level_write_invocation(
        Action<TextWriter> write, 
        string expectedValue)
    {
        using var writer = new ObservableStringWriter();

        using var events = writer.ToSubscribedList();

        write(writer);

        events.Should()
            .ContainSingle()
            .Which
            .Should()
            .Be(expectedValue);
    }

    public static IEnumerable<object[]> WriteOperations()
    {
        foreach (var (writeOperation, expectedValue) in actions())
        {
            yield return new object[] { writeOperation, expectedValue };
        }

        IEnumerable<( Action<TextWriter> writeOperation,  string expectedValue )> actions()
        {
            yield return (writer => writer.Write("char[] buffer, int index, int count".ToArray()   , 0, 4), "char");
            yield return (writer => writer.Write("string value"), "string value");
            yield return (writer => writer.WriteAsync('c').Wait(), "c");
            yield return (writer => writer.WriteAsync(new[] { 'h', 'e', 'l', 'l', 'o' }, 0, 4).Wait(), "hell");
            yield return (writer => writer.WriteAsync("string value").Wait(), "string value");
            yield return (writer => writer.WriteLineAsync('c').Wait(), "c" + NewLine);
            yield return (writer => writer.WriteLineAsync("char[] buffer, int index, int count".ToArray(), 0, 4).Wait(), "char" + NewLine);
            yield return (writer => writer.WriteLineAsync("string value").Wait(), "string value" + NewLine);
            yield return (writer => writer.Write(true), true.ToString());
            yield return (writer => writer.Write("char[] buffer".ToArray()), "char[] buffer");
            yield return (writer => writer.Write(1d), 1d.ToString());
            yield return (writer => writer.Write(2.1), 2.1.ToString());
            yield return (writer => writer.Write(int.MaxValue), int.MaxValue.ToString());
            yield return (writer => writer.Write(long.MaxValue), long.MaxValue.ToString());
            yield return (writer => writer.Write(new object()), new object().ToString());
            yield return (writer => writer.Write(float.MaxValue), float.MaxValue.ToString());
            yield return (writer => writer.Write("string format, object {0}", 123), "string format, object 123");
            yield return (writer => writer.Write("string format, object {0}, object {1}", 456, 789), "string format, object 456, object 789");
            yield return (writer => writer.Write("string format, object {0}, object {1}, object {2}", 12, 34, 56), "string format, object 12, object 34, object 56");
            yield return (writer => writer.Write("string format, object {0}, object {1}, object {2}, object {3}", 123, false, new object(), 789), $"string format, object {123}, object {false}, object {new object()}, object {789}");
            yield return (writer => writer.Write(uint.MaxValue), uint.MaxValue.ToString());
            yield return (writer => writer.Write(ulong.MaxValue), ulong.MaxValue.ToString());
            yield return (writer => writer.WriteLine(), NewLine);
            yield return (writer => writer.WriteLine(true), true + NewLine);
            yield return (writer => writer.WriteLine('c'), "c" + NewLine);
            yield return (writer => writer.WriteLine("char[] buffer".ToArray()), "char[] buffer" + NewLine);
            yield return (writer => writer.WriteLine("char[] buffer, int index, int count".ToArray(), 0, 4), "char" + NewLine);
            yield return (writer => writer.WriteLine(decimal.MaxValue), decimal.MaxValue + NewLine);
            yield return (writer => writer.WriteLine(double.MaxValue), double.MaxValue + NewLine);
            yield return (writer => writer.WriteLine(int.MaxValue), int.MaxValue + NewLine);
            yield return (writer => writer.WriteLine(long.MaxValue), long.MaxValue + NewLine);
            yield return (writer => writer.WriteLine(new object()), new object() + NewLine);
            yield return (writer => writer.WriteLine(float.MaxValue), float.MaxValue + NewLine);
            yield return (writer => writer.WriteLine("string value"), "string value" + NewLine);
                
            yield return (writer => writer.WriteLine("string format, object {0}", 123), "string format, object 123" + NewLine);
            yield return (writer => writer.WriteLine("string format, object {0}, object {1}", 456, 789), "string format, object 456, object 789" + NewLine);
            yield return (writer => writer.WriteLine("string format, object {0}, object {1}, object {2}", 12, 34, 56), "string format, object 12, object 34, object 56" + NewLine);
            yield return (writer => writer.WriteLine("string format, object {0}, object {1}, object {2}, object {3}", 123, false, new object(), 789), $"string format, object {123}, object {false}, object {new object()}, object {789}" + NewLine);
                
            yield return (writer => writer.WriteLine(uint.MaxValue), uint.MaxValue + NewLine);
            yield return (writer => writer.WriteLine(ulong.MaxValue), ulong.MaxValue + NewLine);
            yield return (writer => writer.WriteLineAsync().Wait(), NewLine);
            yield return (writer => writer.Write("ReadOnlySpan<char> buffer".AsSpan()), "ReadOnlySpan<char> buffer");
            yield return (writer => writer.WriteAsync("ReadOnlyMemory<char> buffer, CancellationToken cancellationToken".AsMemory(), CancellationToken.None ).Wait(), "ReadOnlyMemory<char> buffer, CancellationToken cancellationToken");
            yield return (writer => writer.WriteLine("ReadOnlySpan<char> buffer".AsSpan()), "ReadOnlySpan<char> buffer" + NewLine);
            yield return (writer => writer.WriteLineAsync("ReadOnlyMemory<char> buffer, CancellationToken cancellationToken".AsMemory(), CancellationToken.None ).Wait(), "ReadOnlyMemory<char> buffer, CancellationToken cancellationToken" + NewLine);
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