// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestCompletion : KernelCommandBase
    {
        public RequestCompletion(string code, LinePosition position, string targetKernelName = null):base(targetKernelName)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Position = position;
        }

        public string Code { get;  }

        public LinePosition Position { get; }
    }
}
