// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    public class KernelExtensionLoadException : Exception
    {
        public KernelExtensionLoadException(Exception innerException) : base("Failure loading Kernel Extension", innerException)
        {

        }
    }
}