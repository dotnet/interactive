// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.ValueSharing
{
    public interface IKernelValueDeclarer
    {
        bool TryGetValueDeclaration(
            ValueProduced valueProduced, 
            string declareAsName,
            out KernelCommand command);
    }
}