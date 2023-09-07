// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter;

public interface IJupyterMessageResponseSender
{
    void Send(PubSubMessage message);
    void Send(ReplyMessage message);
    string Send(InputRequest message);
}