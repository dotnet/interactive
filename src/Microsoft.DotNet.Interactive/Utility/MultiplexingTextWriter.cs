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
    public class MultiplexingTextWriter : TextWriter
    {
        private static volatile UnicodeEncoding _encoding;

        private readonly Func<TextWriter> _createTextWriter;

        private int _key = 0;

        private readonly AsyncLocal<int?> _localKey = new AsyncLocal<int?>();

        private readonly ConcurrentDictionary<int, TextWriter> _writers = new ConcurrentDictionary<int, TextWriter>();
        
        private readonly TextWriter _defaultWriter;

        public MultiplexingTextWriter(
            Func<TextWriter> createTextWriter = null,
            TextWriter defaultWriter = null)
        {
            _createTextWriter = createTextWriter ?? (() => new StringWriter());
            _defaultWriter = defaultWriter ?? Null;
        }

        public override void Close()
        {
            Dispose(true);
        }

        public IObservable<string> GetObservable()
        {
            // FIX: (GetObservable) observability per context
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

        public override void Write(char[] buffer, int index, int count)
        {
            GetCurrentWriter().Write(buffer, index, count);
        }

        public override void Write(string value)
        {
            if (value != null)
            {
                GetCurrentWriter().Write(value);
            }
        }

        public IDisposable EnsureInitializedForCurrentAsyncContext()
        {
            if (_localKey.Value is null)
            {
                _localKey.Value = Interlocked.Increment(ref _key);

                var copy = _key;

                return Disposable.Create(() =>
                {
                    _writers.TryRemove(copy, out var writer);
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
            if (_localKey.Value is { } key)
            {
                return _writers.GetOrAdd(
                    key,
                    _ => _createTextWriter());
            }

            return _defaultWriter;
        }

        public override Task WriteAsync(char value)
        {
            Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(String value)
        {
            Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            Write(buffer, index, count);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(char value)
        {
            WriteLine(value);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(String value)
        {
            WriteLine(value);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            WriteLine(buffer, index, count);
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            if (_localKey.Value is {} key)
            {
                return _writers[key].ToString();
            }

            return "";
        }
    }
}