// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Events
{
    public class DisplayedValue
    {
        private readonly string _displayId;
        private readonly string _mimeType;
        private readonly KernelInvocationContext _context;

        public DisplayedValue(string displayId, string mimeType, KernelInvocationContext context)
        {
            if (string.IsNullOrWhiteSpace(displayId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(displayId));
            }

            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
            }

            _displayId = displayId;
            _mimeType = mimeType;
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Update(object updatedValue)
        {
            var formatted = new FormattedValue(
                _mimeType,
                updatedValue.ToDisplayString(_mimeType));

            _context.Publish(new DisplayedValueUpdated(updatedValue, _displayId, _context.Command, new[] { formatted }));
        }
    }
}