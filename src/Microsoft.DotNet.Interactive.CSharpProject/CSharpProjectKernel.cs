// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    public class CSharpProjectKernel
    {

    }
    //public class CSharpProjectKernel :
    //    Kernel,
    //    IKernelCommandHandler<CompileProject>,
    //    IKernelCommandHandler<OpenDocument>,
    //    IKernelCommandHandler<OpenProject>,
    //    IKernelCommandHandler<RequestCompletions>,
    //    IKernelCommandHandler<RequestSignatureHelp>
    //{
    //    private WorkspaceProject _workspaceProject;
    //    private Document _currentDocument;
    //    private RoslynWorkspaceServer _workspaceServer;

    //    public CSharpProjectKernel(string name)
    //        : base(name)
    //    {
    //        _workspaceServer = new RoslynWorkspaceServer();
    //    }

    //    public Task HandleAsync(OpenProject command, KernelInvocationContext context)
    //    {
    //        _workspaceProject = WorkspaceProject.Create(command.Project);
    //        return Task.CompletedTask;
    //    }

    //    public Task HandleAsync(OpenDocument command, KernelInvocationContext context)
    //    {
    //        if (_workspaceProject is null)
    //        {
    //            throw new InvalidOperationException($"Cannot open document before project has been opened.  Send the command {nameof(OpenProject)} first");
    //        }

    //        if (string.IsNullOrWhiteSpace(command.RegionName))
    //        {
    //            _currentDocument = _workspaceProject.FindOrCreateDocument(command.Path);
    //        }
    //        else
    //        {
    //            var document = _workspaceProject.FindDocument(command.Path);
    //            if (document is null)
    //            {
    //                throw new Exception($"File '{command.Path}' not found");
    //            }
    //            else
    //            {
    //                var regionSpan = document.FindRegionSpan(command.RegionName);
    //                if (regionSpan is null)
    //                {
    //                    throw new Exception($"Region '{command.RegionName}' not found in file '{command.Path}'");
    //                }

    //                document.RegionSpan = regionSpan;
    //                _currentDocument = document;
    //            }
    //        }

    //        return Task.CompletedTask;

    //        // TODO: publish DocumentOpened, Path="", RegionName="", Contents="// thing"
    //    }

    //    public async Task HandleAsync(CompileProject command, KernelInvocationContext context)
    //    {
    //        if (_currentDocument is null)
    //        {
    //            context.Fail(command, message: $"Cannot compile project before document has been opened.  Send the command {nameof(OpenDocument)} first");
    //            return;
    //        }

    //        var compileResult = await _workspaceServer.Compile(_workspaceProject, null);

    //        var visibleDiagnostics = compileResult.Diagnostics.Where(d => d.IsVisible());
    //        // TODO: create formattedDiagnostics
    //        context.Publish(new DiagnosticsProduced(visibleDiagnostics, command));

    //        if (compileResult.Diagnostics.Any(d => d.Severity == Severity.Error))
    //        {
    //            context.Fail(command, message: "TODO:");
    //            return;
    //        }

    //        context.Publish(new AssemblyProduced(compileResult.Assembly));
    //    }

    //    public Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task HandleAsync(RequestSignatureHelp command, KernelInvocationContext context)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
