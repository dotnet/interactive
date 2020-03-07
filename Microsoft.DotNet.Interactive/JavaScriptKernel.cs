// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public class JavaScriptKernel : KernelBase
    {
        public const string DefaultKernelName = "javascript";

        public JavaScriptKernel() : base(DefaultKernelName)
        {
        }

        public override bool TryGetVariable(string name, out object value)
        {
            value = default;
            return false;
        }

        protected override async Task HandleSubmitCode(
            SubmitCode command,
            KernelInvocationContext context)
        {
            var scriptContent = command.Code;

            string value = PocketViewTags.script[type: "text/javascript"](Kernel.HTML(
                                                                              scriptContent))
                                         .ToString();

            await context.DisplayAsync(value, "text/html");
        }

        protected override Task HandleRequestCompletion(RequestCompletion command, KernelInvocationContext context)
        {
            throw new NotSupportedException();
        }
    }
}