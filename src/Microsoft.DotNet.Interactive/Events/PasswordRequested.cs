// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Events
{
    public class PasswordRequested : KernelEventBase
    {
        [JsonConstructor]
        internal PasswordRequested(string prompt)
        {
            Prompt = prompt;
        }

        public PasswordRequested(
            string prompt,
            KernelCommand command) : base(command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            Prompt = prompt;
        }

        [JsonIgnore]
        public PasswordString Content { get; set; }
        public string Prompt { get; }

        public override string ToString() => $"{nameof(PasswordRequested)}: Prompt-'{Prompt}'";
    }
}
