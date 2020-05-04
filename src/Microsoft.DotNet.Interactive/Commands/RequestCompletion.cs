// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestCompletion : KernelCommandBase
    {
        public RequestCompletion(string code, int cursorPosition, string targetKernelName = null):base(targetKernelName)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            CursorPosition = cursorPosition < 0 ? throw new ArgumentOutOfRangeException(nameof(cursorPosition), "cannot be negative") : cursorPosition;
        }

        public string Code { get;  }

        public int CursorPosition { get; }
    }
}