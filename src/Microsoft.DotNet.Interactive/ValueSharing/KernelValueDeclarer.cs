// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.ValueSharing;

public static class KernelValueDeclarer
{
    public static IKernelValueDeclarer Default { get; } = new DefaultKernelValueDeclarer();

    private class DefaultKernelValueDeclarer : IKernelValueDeclarer
    {
        public bool TryGetValueDeclaration(string valueName, object value, out KernelCommand command)
        {
            command = null;
            return false;
        }
    }
}