// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class CSharpProjectKernel
{
    static CSharpProjectKernel()
    {
        // register commands and event with serialization
        KernelCommandEnvelope.RegisterCommand<OpenProject>();
        KernelCommandEnvelope.RegisterCommand<OpenDocument>();
        KernelCommandEnvelope.RegisterCommand<CompileProject>();

        KernelEventEnvelope.RegisterEvent<DocumentOpened>();
        KernelEventEnvelope.RegisterEvent<AssemblyProduced>();
    }
}