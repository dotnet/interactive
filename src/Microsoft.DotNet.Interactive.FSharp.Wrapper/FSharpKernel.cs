// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.FSharp
{
    // The purpose of this class is to enable interface implementation with different generic instantiations; can be removed
    // once https://github.com/dotnet/fsharp/pull/2867 is complete.
    public class FSharpKernel :
        FSharpKernelBase,
        IKernelCommandHandler<RequestCompletions>,
        IKernelCommandHandler<RequestDiagnostics>,
        IKernelCommandHandler<SubmitCode>,
        IKernelCommandHandler<RequestHoverText>
    {
        public Task HandleAsync(RequestCompletions command, KernelInvocationContext context) => HandleRequestCompletionAsync(command, context);

        public Task HandleAsync(RequestDiagnostics command, KernelInvocationContext context) => HandleRequestDiagnosticsAsync(command, context);

        public Task HandleAsync(SubmitCode command, KernelInvocationContext context) => HandleSubmitCodeAsync(command, context);

        public Task HandleAsync(RequestHoverText command, KernelInvocationContext context) => HandleRequestHoverText(command, context);
    }
}
