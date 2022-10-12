// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

internal interface IKernelCommandToMessageHandler<TCommand> where TCommand: KernelCommand
{
    Task HandleCommandAsync(TCommand command, ICommandExecutionContext context, CancellationToken token);
}
