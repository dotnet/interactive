// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public static class BufferExtensions
{
    public static Buffer GetBufferWithSpecifiedIdOrSingleBufferIfThereIsOnlyOne(
        this Workspace workspace,
        BufferId bufferId = null)
    {
        // TODO: (GetBufferWithSpecifiedIdOrSingleBufferIfThereIsOnlyOne) this concept should go away

        var buffer = workspace.Buffers.SingleOrDefault(b => b.Id == bufferId);

        if (buffer == null)
        {
            if (workspace.Buffers.Length == 1)
            {
                buffer = workspace.Buffers.Single();
            }
            else
            {
                throw new ArgumentException("Ambiguous buffer");
            }
        }

        return buffer;
    }
}