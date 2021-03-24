// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Utility.MultiplexingTextWriter>;
namespace Microsoft.DotNet.Interactive.Utility
{
    public class MultiplexingTextWriter : TextWriter
    {
        private static volatile UnicodeEncoding _encoding;

        private readonly Func<TextWriter> _createTextWriter;
        private readonly string _name;
        private readonly object _lockObj = new();
        private readonly ConcurrentDictionary<int, (int refCount, TextWriter writer)> _writers = new();

        private readonly TextWriter _defaultWriter;
        private bool _disposed = false;

        public MultiplexingTextWriter(
            string name,
            Func<TextWriter> createTextWriter = null,
            TextWriter defaultWriter = null)
        {   
            _createTextWriter = createTextWriter ?? DefaultCreateTextWriter;
            _name = name;
            _defaultWriter = defaultWriter ?? Null;
        }

        private TextWriter DefaultCreateTextWriter()
        {
            return new ObservableStringWriter(_name + ":" + AsyncContext.Id);
        }

        public IDisposable EnsureInitializedForCurrentAsyncContext()
        {
            AsyncContext.TryEstablish(out var id);

            return Disposable.Create(() =>
            {
                // FIX: (EnsureInitializedForCurrentAsyncContext) ref count?
                lock (_lockObj)
                {
                    var success = _writers.TryGetValue(id, out var writer);

                    if (success)
                    {
                        if (writer.refCount == 1)
                        {
                            Log.Info($"Disposing {{name}}:{GetHashCode()} on asyncContextId {{id}}.", _name, id, success);
                            writer.writer.Dispose();
                            _writers.TryRemove(id, out _);
                        }
                        else
                        {
                            Log.Info($"Reducing ref count for {{name}}:{GetHashCode()} on asyncContextId {{id}} to {{refcount}}.", _name, id, success, writer.refCount);
                            _writers[id] = (writer.refCount - 1, writer.writer);
                        }
                    }
                    else
                    {
                        Log.Error(
                            message: $"Couldn't find {{name}}:{GetHashCode()} on asyncContextId {{id}}", 
                            args: new object[]{_name, id});
                    }
                }
            });
        }

        private TextWriter GetCurrentWriter(bool forWrite = true)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException($"{nameof(MultiplexingTextWriter)} {_name} has been disposed.");
            }

            EnsureInitializedForCurrentAsyncContext();

            string readOrWrite;
            if (forWrite)
            {
                readOrWrite = "write";
            }
            else
            {
                readOrWrite = "read";
            }

            if (AsyncContext.Id is { } asyncContextId)
            {
                // FIX: (GetCurrentWriter) remove debuggy stuff

                lock (_lockObj)
                {
                    var writer = _writers.GetOrAdd(
                        asyncContextId,
                        _ =>
                        {
                            Log.Info($"Adding writer {{name}}:{GetHashCode()} on {{asyncContextId}} for {{readOrWrite}}", _name, asyncContextId, readOrWrite);
                            return (0, _createTextWriter());
                        });

                    _writers[asyncContextId] = (writer.refCount + 1, writer.writer);

                    Log.Info($"Retrieving {{name}}:{GetHashCode()} on {{asyncContextId}} for {{readOrWrite}}. Writers: {{writers}}.", _name, asyncContextId, readOrWrite, _writers);

                    return writer.writer;
                }
            }

            return _defaultWriter;
        }

        public IObservable<string> GetObservable()
        {
            if (GetCurrentWriter(false) is IObservable<string> observable)
            {
                return observable;
            }

            // FIX: (GetObservable) needed?
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

        public IEnumerable<TextWriter> Writers => _writers.Select(w => w.Value.writer);

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

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
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

        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new())
        {
            return GetCurrentWriter().WriteLineAsync(buffer, cancellationToken);
        }

        public override Task WriteLineAsync(string value)
        {
            return GetCurrentWriter().WriteLineAsync(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposed = true;
                var writers = _writers.Values.ToArray();
                _writers.Clear();
                foreach (var writer in writers)
                {
                    writer.writer.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public override string ToString()
        {
            if (AsyncContext.Id is { } asyncContextId &&
                _writers.TryGetValue(asyncContextId, out var writer))
            {
                Log.Info($"ToString {{name}}:{GetHashCode()} on {{asyncContextId}}", _name, asyncContextId);

                return writer.writer.ToString();
            }

            return "";
        }
    }
}