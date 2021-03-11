// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public abstract class FrontendEnvironment
    {
        public bool AllowStandardInput { get; set; }

        public virtual Task ExecuteClientScript(string code, KernelInvocationContext context)
        {
            return Task.CompletedTask;
        }
    }
}
