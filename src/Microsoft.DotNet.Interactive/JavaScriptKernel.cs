// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive;

public class JavaScriptKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>,
    IKernelCommandHandler<SendValue>
{
    private readonly KernelClientBase _client;
    public const string DefaultKernelName = "javascript";

    public JavaScriptKernel(KernelClientBase client = null) : base(DefaultKernelName)
    {
        _client = client;
    }

    Task IKernelCommandHandler<SubmitCode>.HandleAsync(
        SubmitCode command,
        KernelInvocationContext context)
    {
        return FrontendEnvironment.ExecuteClientScript(command.Code, context);
    }

    protected override Func<TCommand, KernelInvocationContext, Task> CreateDefaultHandlerForCommandType<TCommand>()
    {
        return (kernelCommand, _) => ForwardCommand(kernelCommand);
    }

    private Task ForwardCommand(KernelCommand command)
    {
        return _client?.SendAsync(command) ?? Task.CompletedTask;
    }

    public async Task HandleAsync(SendValue command, KernelInvocationContext context)
    {
        if (JavaScriptValueDeclarer.TryGetValueDeclaration(command.Value, command.Name, out var code))
        {
            await FrontendEnvironment.ExecuteClientScript(code, context);
        }
    }
}