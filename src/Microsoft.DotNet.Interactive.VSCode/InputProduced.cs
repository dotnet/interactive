﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.VSCode
{
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
