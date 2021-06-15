// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public static class KernelExtensions
    {
        public static CSharpKernel UseVSCodeHelpers(this CSharpKernel kernel)
        {
            var command = new SubmitCode($@"
#r ""{typeof(VSCodeInteractiveHost).Assembly.Location.Replace("\\", "/")}""
{typeof(IInteractiveHost).FullName} host = new {typeof(VSCodeInteractiveHost).FullName}();
");
            kernel.DeferCommand(command);
            return kernel;
        }

        public static FSharpKernel UseVSCodeHelpers(this FSharpKernel kernel)
        {
            var command = new SubmitCode($@"
#r ""{typeof(VSCodeInteractiveHost).Assembly.Location.Replace("\\", "/")}""
let host = {typeof(VSCodeInteractiveHost).FullName}() :> {typeof(IInteractiveHost).FullName}
");
            kernel.DeferCommand(command);
            return kernel;
        }
    }
}
