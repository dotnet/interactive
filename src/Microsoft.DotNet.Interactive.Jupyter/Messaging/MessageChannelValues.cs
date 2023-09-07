// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging;

internal class MessageChannelValues
{
    public const string shell = nameof(shell);

    public const string control = nameof(control);

    public const string iopub = nameof(iopub);

    public const string stdin = nameof(stdin);
}