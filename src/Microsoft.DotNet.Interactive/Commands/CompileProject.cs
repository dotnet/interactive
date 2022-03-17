// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class CompileProject : KernelCommand
    {
        public string Code { get; }

        public CompileProject(string code = null)
        {
            Code = code;
        }
    }
}
