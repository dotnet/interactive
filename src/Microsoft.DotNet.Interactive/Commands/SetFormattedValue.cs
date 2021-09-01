// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class SetFormattedValue : KernelCommand
    {
        public string FormattedValue { get; }
        public string MimeType { get; }
        public string Name { get; }

        public SetFormattedValue(string formattedValue, string mimeType, string name, string targetKernelName) : base(targetKernelName)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }
            FormattedValue = formattedValue ?? throw new ArgumentNullException(nameof(formattedValue));

            MimeType = mimeType;
            Name = name;
        }
    }
}