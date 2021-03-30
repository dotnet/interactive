// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MermaidMarkdown
    {
        public override string ToString()
        {
            return _value;
        }

        private readonly string _value;

        public MermaidMarkdown(string value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}