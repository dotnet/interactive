// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MermaidKernel : Kernel
        , IKernelCommandHandler<SubmitCode>

    {
        public MermaidKernel() : base("mermaid")
        {
        }

        public Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            var markdown = new MermaidMarkdown(command.Code);
            context.Display(markdown);
            return Task.CompletedTask;
        }
    }
}