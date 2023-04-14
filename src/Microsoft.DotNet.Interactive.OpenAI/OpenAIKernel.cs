// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class OpenAIKernel : 
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    public OpenAIKernel(string name = "openai") : base(name)
    {
        KernelInfo.LanguageName = "text";
        }



    public Task HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        throw new NotImplementedException();
    }
}