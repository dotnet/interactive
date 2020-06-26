// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.DotNet.Interactive.SignalR
{
    public class SignalROutputTextStream : OutputTextStream
    {
        private readonly HubConnection _connection;

        public SignalROutputTextStream(HubConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
        protected override void WriteText(string text)
        {
            _connection.SendAsync("submitCommand", text);
        }
    }
}