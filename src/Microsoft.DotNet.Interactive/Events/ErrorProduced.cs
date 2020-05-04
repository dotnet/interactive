// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class ErrorProduced : DisplayEventBase
    {
        public ErrorProduced(
            string message,
            IKernelCommand command = null,
            IReadOnlyCollection<FormattedValue> formattedValues = null) : base(message, command, formattedValues)
        {
            Message = message;
        }

        public string Message { get; }
    }
}