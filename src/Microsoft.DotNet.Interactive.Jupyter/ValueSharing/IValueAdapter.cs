// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using System;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

internal interface IValueAdapter : IDisposable, 
    IKernelCommandToMessageHandler<SendValue>, 
    IKernelCommandToMessageHandler<RequestValue>,
    IKernelCommandToMessageHandler<RequestValueInfos>
{
}
