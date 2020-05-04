// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using NetMQ;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    internal class TextSocket : IOutgoingSocket
    {
        readonly StringBuilder _buffer = new StringBuilder();

        public bool TrySend(ref Msg msg, TimeSpan timeout, bool more)
        {
            var decoded = SendReceiveConstants.DefaultEncoding.GetString(msg.Data);
            _buffer.AppendLine($"data: {decoded} more: {more}");
            return true;
        }

        public string GetEncodedMessage()
        {
            return _buffer.ToString();
        }
    }
}