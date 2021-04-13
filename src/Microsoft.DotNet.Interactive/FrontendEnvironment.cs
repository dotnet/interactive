// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public abstract class FrontendEnvironment
    {
        public bool AllowStandardInput { get; set; }

        public virtual Task ExecuteClientScript(string code, KernelInvocationContext context)
        {
            var scriptContent = new ScriptContent(code);

            context.Display(scriptContent);

            return Task.CompletedTask;
        }

        public virtual Task ForwardCommand(KernelCommand command, KernelInvocationContext context)
        {
            throw new NotSupportedException();
        }
    }
}
