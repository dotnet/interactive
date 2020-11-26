// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.DotNet.Interactive.Utility
{
    internal static class AsyncContext
    {
        private static int _seed = 0;

        private static readonly AsyncLocal<int?> _id = new AsyncLocal<int?>();

        public static int? Id
        {
            get => _id.Value;
            set => _id.Value = value;
        }

        public static bool TryEstablish(out int id)
        {
            if (_id.Value is { } value)
            {
                id = _id.Value.Value;
                return false;
            }
            else
            {
                _id.Value = Interlocked.Increment(ref _seed);
                id = _id.Value.Value;
                return true;
            }
        }
    }

    public class MultiplexingTextWriter : TextWriter
    {
        private static volatile UnicodeEncoding _encoding;

        private readonly Func<TextWriter> _createTextWriter;

        private readonly ConcurrentDictionary<int, TextWriter> _writers = new ConcurrentDictionary<int, TextWriter>();

        private readonly TextWriter _defaultWriter;

        public MultiplexingTextWriter(
            Func<TextWriter> createTextWriter = null,
            TextWriter defaultWriter = null)
        {
            _createTextWriter = createTextWriter ?? (() => new ObservableStringWriter());
            _defaultWriter = defaultWriter ?? new ObservableStringWriter();
        }

        public override void Close()
        {
            Dispose(true);
        }

        public IDisposable EnsureInitializedForCurrentAsyncContext()
        {
            if (AsyncContext.TryEstablish(out var id))
            {
                return Disposable.Create(() =>
                {
                    _writers.TryRemove(id, out var writer);
                    writer?.Dispose();
                });
            }
            else
            {
                return Disposable.Empty;
            }
        }

        private TextWriter GetCurrentWriter()
        {
            if (AsyncContext.Id is { } key)
            {
                return _writers.GetOrAdd(
                    key,
                    _ => _createTextWriter());
            }

            return _defaultWriter;
        }

        public IObservable<string> GetObservable()
        {
            if (GetCurrentWriter() is IObservable<string> observable)
            {
                if (_defaultWriter is IObservable<string> observable2)
                {
                    return observable2.Merge(observable);
                }

                return observable;
            }

            if (_defaultWriter is IObservable<string> observable3)
            {
                return observable3;
            }

            return Observable.Empty<string>();
        }

        public override Encoding Encoding
        {
            get
            {
                if (_encoding == null)
                {
                    _encoding = new UnicodeEncoding(false, false);
                }

                return _encoding;
            }
        }

        public IEnumerable<TextWriter> Writers => _writers.Values;

        public override void Write(char value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void WriteLine()
        {
            GetCurrentWriter().WriteLine();
        }

        public override void WriteLine(char value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            GetCurrentWriter().Write(buffer, index, count);
        }

        public override void Write(string value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void Write(bool value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void Write(char[] buffer)
        {
            GetCurrentWriter().Write(buffer);
        }

        public override void Write(decimal value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void Write(double value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void Write(int value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void Write(long value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void Write(object value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
            GetCurrentWriter().Write(buffer);
        }

        public override void Write(float value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void Write(string format, object arg0)
        {
            GetCurrentWriter().Write(format, arg0);
        }

        public override void Write(string format, object arg0, object arg1)
        {
            GetCurrentWriter().Write(format, arg0, arg1);
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            GetCurrentWriter().Write(format, arg0, arg1, arg2);
        }

        public override void Write(string format, params object[] arg)
        {
            GetCurrentWriter().Write(format, arg);
        }

        public override void Write(uint value)
        {
            GetCurrentWriter().Write(value);
        }

        public override void Write(ulong value)
        {
            GetCurrentWriter().Write(value);
        }

        public override Task WriteAsync(char value)
        {
            return GetCurrentWriter().WriteAsync(value);
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            return GetCurrentWriter().WriteAsync(buffer, index, count);
        }

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return GetCurrentWriter().WriteAsync(buffer, cancellationToken);
        }

        public override Task WriteAsync(string value)
        {
            return GetCurrentWriter().WriteAsync(value);
        }

        public override void WriteLine(bool value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void WriteLine(char[] buffer)
        {
            GetCurrentWriter().WriteLine(buffer);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            GetCurrentWriter().WriteLine(buffer, index, count);
        }

        public override void WriteLine(decimal value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void WriteLine(double value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void WriteLine(int value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void WriteLine(long value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void WriteLine(object value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            GetCurrentWriter().WriteLine(buffer);
        }

        public override void WriteLine(float value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void WriteLine(string value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void WriteLine(string format, object arg0)
        {
            GetCurrentWriter().WriteLine(format, arg0);
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            GetCurrentWriter().WriteLine(format, arg0, arg1);
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            GetCurrentWriter().WriteLine(format, arg0, arg1, arg2);
        }

        public override void WriteLine(string format, params object[] arg)
        {
            GetCurrentWriter().WriteLine(format, arg);
        }

        public override void WriteLine(uint value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override void WriteLine(ulong value)
        {
            GetCurrentWriter().WriteLine(value);
        }

        public override Task WriteLineAsync()
        {
            return GetCurrentWriter().WriteLineAsync();
        }

        public override Task WriteLineAsync(char value)
        {
            return GetCurrentWriter().WriteLineAsync(value);
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            return GetCurrentWriter().WriteLineAsync(buffer, index, count);
        }

        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return GetCurrentWriter().WriteLineAsync(buffer, cancellationToken);
        }

        public override Task WriteLineAsync(string value)
        {
            return GetCurrentWriter().WriteLineAsync(value);
        }

        public override string ToString()
        {
            if (AsyncContext.Id is { } key)
            {
                return _writers[key].ToString();
            }

            return "";
        }
    }
}