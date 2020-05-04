// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class DisplayError : KernelCommandBase
    {
        public DisplayError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));
            }

            Message = message;

            Handler = (_, context) =>
            {
                context.Publish(new ErrorProduced(Message));

                return Task.CompletedTask;
            };
        }

        public string Message { get; }
    }
}