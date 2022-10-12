// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

internal class DefaultKernelValueDeclarer : IKernelValueDeclarer
{
    public bool TryGetValueDeclaration(ValueProduced valueProduced, string declareAsName, out KernelCommand command)
    {
        command = new SetValue(valueProduced.Value, declareAsName ?? valueProduced.Name, valueProduced.FormattedValue);
        return true;
    }
}
