// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CommandSucceeded : KernelEvent
    {
        [JsonConstructor]
        internal CommandSucceeded()
        {
        }

        public CommandSucceeded(KernelCommand command) : base(command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
        }

        public override string ToString() => $"{nameof(CommandSucceeded)}: {Command}";
    }
}