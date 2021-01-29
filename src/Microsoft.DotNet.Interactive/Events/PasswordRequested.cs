// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class PasswordRequested : KernelEvent
    {
        public PasswordRequested(
            string prompt,
            KernelCommand command) : base(command)
        {
            Prompt = prompt;
        }

        [JsonIgnore]
        public PasswordString Content { get; set; }
        public string Prompt { get; }

        public override string ToString() => $"{nameof(PasswordRequested)}: Prompt-'{Prompt}'";
    }
}
