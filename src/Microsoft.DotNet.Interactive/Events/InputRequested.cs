// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Events
{
    public class InputRequested : KernelEventBase
    {
        [JsonConstructor]
        internal InputRequested(string prompt)
        {
            Prompt = prompt;
        }

        public InputRequested(
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
        public string Content { get; set; }
        public string Prompt { get; }

        public override string ToString() => $"{nameof(InputRequested)}: Prompt-'{Prompt}'";
    }
}
