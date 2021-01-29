// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public abstract class KernelEvent
    {
        protected KernelEvent(KernelCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            Command = command;
        }


        [JsonIgnore]
        public KernelCommand Command { get; }

        public override string ToString()
        {
            return $"{GetType().Name}";
        }
    }
}