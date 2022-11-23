// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    public class FakeKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>
    {
        public FakeKernel([CallerMemberName] string name = null, string languageName = null, string displayName = null) : base(name)
        {
            KernelInfo.LanguageName = languageName ?? name;
            KernelInfo.DisplayName = displayName ?? name;
        }

        public KernelCommandInvocation Handle { get; set; }

        Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            Handle?.Invoke(command, context);
            return Task.CompletedTask;
        }
    }
}
