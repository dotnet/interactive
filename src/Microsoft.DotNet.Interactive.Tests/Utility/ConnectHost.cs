// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public static class ConnectHost
{
    public static CompositeKernel ConnectInProcessHost(
        this CompositeKernel localCompositeKernel,
        Uri uri = null)
    {
        CompositeKernel remoteCompositeKernel = new();

        localCompositeKernel.RegisterForDisposal(remoteCompositeKernel);

        ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel,
            uri ?? new Uri("kernel://local/"),
            new Uri("kernel://remote/"));

        return localCompositeKernel;
    }
    
    public static void ConnectInProcessHost(
        CompositeKernel localCompositeKernel,
        CompositeKernel remoteCompositeKernel,
        Uri localHostUri = null,
        Uri remoteHostUri = null)
    {
        localHostUri ??= new("kernel://local");
        remoteHostUri ??= new("kernel://remote");

        var localSenderSubject = new Subject<string>();
        var remoteSenderSubject = new Subject<string>();

        var localReceiver = KernelCommandAndEventReceiver.FromObservable(remoteSenderSubject);
        var remoteReceiver = KernelCommandAndEventReceiver.FromObservable(localSenderSubject);

        var localToRemoteSender = KernelCommandAndEventSender.FromObserver(
            localSenderSubject,
            remoteHostUri);
        var remoteToLocalSender = KernelCommandAndEventSender.FromObserver(
            remoteSenderSubject,
            localHostUri);

        var localHost = localCompositeKernel.UseHost(
            localToRemoteSender,
            localReceiver,
            localHostUri);

        var remoteHost = remoteCompositeKernel.UseHost(
            remoteToLocalSender,
            remoteReceiver,
            remoteHostUri);

        Task.Run(async () =>
        {
            await localHost.ConnectAsync();
            await remoteHost.ConnectAsync();
        }).Wait();

        localCompositeKernel.RegisterForDisposal(localHost);
        remoteCompositeKernel.RegisterForDisposal(remoteHost);
        
        localCompositeKernel.RegisterForDisposal(localReceiver);
        remoteCompositeKernel.RegisterForDisposal(remoteReceiver);
    }

    private class TestConsoleStream : Stream
    {
        private readonly MemoryStream _innerStream;

        public TestConsoleStream()
        {
            _innerStream = new MemoryStream();
        }

        public override void Flush()
        {
            lock (_innerStream)
            {
                _innerStream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_innerStream)
            {
                return _innerStream.Read(buffer, offset, count);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (_innerStream)
            {
                return _innerStream.Seek(offset, origin);
            }
        }

        public override void SetLength(long value)
        {
            lock (_innerStream)
            {
                _innerStream.SetLength(value);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_innerStream)
            {
                var pos = Position;
                _innerStream.Write(buffer, offset, count);
                Position = pos;
            }
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}