// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject;

namespace Microsoft.DotNet.Interactive.Events
{
    public class AssemblyProduced : KernelEvent
    {
        public Base64EncodedAssembly Assembly { get; }

        public AssemblyProduced(CompileProject command, Base64EncodedAssembly assembly)
            : base(command)
        {
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }
    }
}
