// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive;

/// <summary>
/// The JavaScript kernel used for frontends where the JavaScript-based kernel is not available.
/// </summary>
/// <remarks>This kernel uses <code>eval()</code> to run JavaScript code in the browser but does not support as many commands as the JavaScript-based kernel.</remarks>
public class JavaScriptKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>,
    IKernelCommandHandler<SendValue>
{
    private readonly KernelClientBase _client;
    public const string DefaultKernelName = "javascript";
    private const string LanguageName = "JavaScript";

    public JavaScriptKernel(KernelClientBase client = null) : base(DefaultKernelName)
    {
        _client = client;
        KernelInfo.LanguageName = LanguageName;
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