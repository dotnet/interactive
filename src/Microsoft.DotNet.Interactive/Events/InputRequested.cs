// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class InputRequested : KernelEvent
    {
        public InputRequested(
            string prompt,
            KernelCommand command) : base(command)
        {
            Prompt = prompt;
        }

        [JsonIgnore]
        public string Content { get; set; }
        public string Prompt { get; }

        public override string ToString() => $"{nameof(InputRequested)}: Prompt-'{Prompt}'";
    }
}
