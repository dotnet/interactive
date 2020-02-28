// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public interface IKernel : IDisposable
    {
        string Name { get; }

        IObservable<IKernelEvent> KernelEvents { get; }

        Task<IKernelCommandResult> SendAsync(
            IKernelCommand command, 
            CancellationToken cancellationToken);

        IReadOnlyCollection<ICommand> Directives { get; }
    }
}