﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Events
{
    public abstract class KernelEvent
    {
        protected KernelEvent(KernelCommand command = null)
        {
            Command = command ?? KernelInvocationContext.Current?.Command;
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public KernelCommand Command { get; internal set; }

        public override string ToString()
        {
            return $"{GetType().Name}";
        }
    }
}