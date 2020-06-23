// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public class HtmlKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>
    {
        public const string DefaultKernelName = "html";

        public HtmlKernel() : base(DefaultKernelName)
        {
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            await context.DisplayAsync(
                command.Code,
                HtmlFormatter.MimeType);
        }
    }
}