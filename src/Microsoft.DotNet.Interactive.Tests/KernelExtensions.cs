// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Tests
{
    public static class KernelExtensions
    {
        public static IKernel FindKernel(this IKernel kernel, Language name)
        {
            return name switch
            {
                Language.CSharp => kernel.FindKernel("csharp"),
                Language.FSharp => kernel.FindKernel("fsharp"),
                Language.PowerShell => kernel.FindKernel("pwsh"),
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
            };
        }
    }
}