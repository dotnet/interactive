// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public class GetInput : KernelCommand
    {
        public string Prompt { get; }
        public bool IsPassword { get; }

        public GetInput(string prompt = "", bool isPassword = false, string targetKernelName = null)
            : base(targetKernelName)
        {
            Prompt = prompt;
            IsPassword = isPassword;
        }
    }

    public class InputProduced : KernelEvent
    {
        public string Value { get; }

        public InputProduced(string value, GetInput command)
            : base(command)
        {
            Value = value;
        }
    }
}
