// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public class JavaScriptKernel : KernelBase
    {
        public const string DefaultKernelName = "javascript";

        public JavaScriptKernel() : base(DefaultKernelName)
        {
        }
        protected override async Task HandleSubmitCode(
            SubmitCode command,
            KernelInvocationContext context)
        {
            var scriptContent = new ScriptContent(command.Code);

            await context.DisplayAsync(scriptContent);
        }

        protected override Task HandleRequestCompletion(RequestCompletion command, KernelInvocationContext context)
        {
            throw new NotSupportedException();
        }
    }
}
