// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.ValueSharing;

public class CSharpValueDeclarer : IKernelValueDeclarer
{
    public bool TryGetValueDeclaration(
        ValueProduced valueProduced,
        out KernelCommand command)
    {
        if (valueProduced.Value is {} value)
        {
            
        }



        command = null;
        return false;
    }
}