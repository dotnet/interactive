// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;

internal interface ICommTarget
{
    public string Name { get; }

    public void OnCommOpen(CommAgent commAgent, object data);
}
