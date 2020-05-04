// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public abstract class DisplayEventBase : KernelEventBase
    {
        protected DisplayEventBase(
            object value,
            IKernelCommand command = null,
            IReadOnlyCollection<FormattedValue> formattedValues = null,
            string valueId = null) : base(command)
        {
            Value = value;
            FormattedValues = formattedValues ?? Array.Empty<FormattedValue>();
            ValueId = valueId;
        }

        public object Value { get; }

        public IReadOnlyCollection<FormattedValue> FormattedValues { get; }

        public string ValueId { get; }

        public override string ToString() => $"{GetType().Name}: {Value?.ToString().TruncateForDisplay()}";
    }
}