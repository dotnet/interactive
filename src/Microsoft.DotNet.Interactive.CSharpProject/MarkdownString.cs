// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public class MarkdownString
    {
        public string Value { get; }
        public bool IsTrusted { get; }

        public MarkdownString(string value, bool isTrusted = false)
        {
            Value = value ?? "";
            IsTrusted = isTrusted;
        }

        public static implicit operator MarkdownString(string  value)  
        {
            return new MarkdownString(value);
        }
    }
}