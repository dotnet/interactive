// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public static class DotNetKernelExtensions
    {
        public static Task SetVariableAsync<T>(this DotNetKernel kernel, string name, T value)
        {
            return kernel.SetVariableAsync(name, value, typeof(T));
        }
    }
}
