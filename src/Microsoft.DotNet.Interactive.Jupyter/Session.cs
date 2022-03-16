// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class Session
{
    public string Id { get; }

    public string Key { get; set; }

    public string Username { get; }

    public string ProtocolVersion => Constants.MESSAGE_PROTOCOL_VERSION;

    public Session(string username = null)
    {
        Id = Guid.NewGuid().ToString();
        Username = username ?? Constants.USERNAME;
    }
}