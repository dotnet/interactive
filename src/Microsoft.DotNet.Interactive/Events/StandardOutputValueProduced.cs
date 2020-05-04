// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class StandardOutputValueProduced : DisplayEventBase
    {
        public StandardOutputValueProduced(
            object value,
            IKernelCommand command,
            IReadOnlyCollection<FormattedValue> formattedValues = null,
            string valueId = null) : base(value, command, formattedValues, valueId)
        {
        }
    }
}