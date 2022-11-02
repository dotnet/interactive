// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class KernelStartException : Exception
    {
        public KernelStartException(string kernelType, string reason): base($"Kernel {kernelType} failed to start. {reason}")
        {
        }
    }
}
