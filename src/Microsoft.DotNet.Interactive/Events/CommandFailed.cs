// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CommandFailed : KernelEvent
    {
        public CommandFailed(
            Exception exception,
            KernelCommand command,
            string message = null) : base(command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            Exception = exception;

            Message = string.IsNullOrWhiteSpace(message) 
                          ? exception?.ToString() ?? $"Command failed: {Command}"
                          : message;
        }

        public CommandFailed(
            string message,
            KernelCommand command) : this(null, command, message)
        {
        }

        [JsonIgnore]
        public Exception Exception { get; }

        public string Message { get; set; }

        public override string ToString() => $"{base.ToString()}: {Message}";
    }
}