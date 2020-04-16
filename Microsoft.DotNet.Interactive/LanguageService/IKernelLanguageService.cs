// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.LanguageService
{
    public interface IKernelLanguageService<TCommand> where TCommand: LanguageServiceCommandBase
    {
        Task Handle(TCommand command, KernelInvocationContext context);
    }
}
