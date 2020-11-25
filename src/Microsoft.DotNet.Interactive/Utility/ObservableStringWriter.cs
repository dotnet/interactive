// // Copyright (c) .NET Foundation and contributors. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Utility
{
    public class ObservableStringWriter : StringWriter, IObservable<string>
    {
        private class Region
        {
            public int Start { get; set; }
            public int Length { get; set; }
        }

        private readonly Subject<string> _writeEvents = new Subject<string>();
        private readonly List<Region> _regions = new List<Region>();
        private bool _trackingWriteOperation;
        private int _observerCount;

        private readonly CompositeDisposable _disposable;

        public ObservableStringWriter()
        {
            _disposable = new CompositeDisposable
            {
                _writeEvents
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposable.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void Write(char value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        private void PublishStringIfObserved(StringBuilder sb, Region region)
        {
            if (_observerCount > 0)
            {
                _writeEvents.OnNext(sb.ToString(region.Start, region.Length));
            }
        }

        private void TrackWriteOperation(Action action)
        {
            if (_trackingWriteOperation)
            {
                action();
                return;
            }

            _trackingWriteOperation = true;
            var sb = base.GetStringBuilder();

            var region = new Region
            {
                Start = sb.Length
            };

            _regions.Add(region);

            action();

            region.Length = sb.Length - region.Start;
            _trackingWriteOperation = false;
            PublishStringIfObserved(sb, region);
        }

        private async Task TrackWriteOperationAsync(Func<Task> action)
        {
            if (_trackingWriteOperation)
            {
                await action();
                return;
            }

            _trackingWriteOperation = true;
            var sb = base.GetStringBuilder();

            var region = new Region
            {
                Start = sb.Length
            };

            _regions.Add(region);

            await action();

            region.Length = sb.Length - region.Start;

            _trackingWriteOperation = false;

            PublishStringIfObserved(sb, region);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            TrackWriteOperation(() => base.Write(buffer, index, count));
        }

        public override void Write(string value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override Task WriteAsync(char value)
        {
            return TrackWriteOperationAsync(() => base.WriteAsync(value));
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            return TrackWriteOperationAsync(() => base.WriteAsync(buffer, index, count));
        }

        public override Task WriteAsync(string value)
        {
            return TrackWriteOperationAsync(() => base.WriteAsync(value));
        }

        public override Task WriteLineAsync(char value)
        {
            return TrackWriteOperationAsync(() => base.WriteLineAsync(value));
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            return TrackWriteOperationAsync(() => base.WriteLineAsync(buffer, index, count));
        }

        public override Task WriteLineAsync(string value)
        {
            return TrackWriteOperationAsync(() => base.WriteLineAsync(value));
        }

        public override void Write(bool value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override void Write(char[] buffer)
        {
            TrackWriteOperation(() => base.Write(buffer));
        }

        public override void Write(decimal value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override void Write(double value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override void Write(int value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override void Write(long value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override void Write(object value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override void Write(float value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override void Write(string format, object arg0)
        {
            TrackWriteOperation(() => base.Write(format, arg0));
        }

        public override void Write(string format, object arg0, object arg1)
        {
            TrackWriteOperation(() => base.Write(format, arg0, arg1));
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            TrackWriteOperation(() => base.Write(format, arg0, arg1, arg2));
        }

        public override void Write(string format, params object[] arg)
        {
            TrackWriteOperation(() => base.Write(format, arg));
        }

        public override void Write(uint value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override void Write(ulong value)
        {
            TrackWriteOperation(() => base.Write(value));
        }

        public override void WriteLine()
        {
            TrackWriteOperation(() => base.WriteLine());
        }

        public override void WriteLine(bool value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(char value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(char[] buffer)
        {
            TrackWriteOperation(() => base.WriteLine(buffer));
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            TrackWriteOperation(() => base.WriteLine(buffer, index, count));
        }

        public override void WriteLine(decimal value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(double value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(int value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(long value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(object value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(float value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(string value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(string format, object arg0)
        {
            TrackWriteOperation(() => base.WriteLine(format, arg0));
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            TrackWriteOperation(() => base.WriteLine(format, arg0, arg1));
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            TrackWriteOperation(() => base.WriteLine(format, arg0, arg1, arg2));
        }

        public override void WriteLine(string format, params object[] arg)
        {
            TrackWriteOperation(() => base.WriteLine(format, arg));
        }

        public override void WriteLine(uint value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override void WriteLine(ulong value)
        {
            TrackWriteOperation(() => base.WriteLine(value));
        }

        public override Task WriteLineAsync()
        {
            return TrackWriteOperationAsync(() => base.WriteLineAsync());
        }

        public IEnumerable<string> Writes()
        {
            var src = base.GetStringBuilder().ToString();
            foreach (var region in _regions)
            {
                yield return src.Substring(region.Start, region.Length);
            }
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
             base.Write(buffer);
        }

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return base.WriteAsync(buffer, cancellationToken);
        }

        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            base.WriteLine(buffer);
        }

        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return base.WriteLineAsync(buffer, cancellationToken);
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            Interlocked.Increment(ref _observerCount);
            return new CompositeDisposable
            {
                Disposable.Create(() => Interlocked.Decrement(ref _observerCount)),
                _writeEvents.Subscribe(observer)
            };
        }
    }
}