// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestHoverText : KernelCommandBase
    {
        public string DocumentIdentifier { get; }
        public LinePosition Position { get; }

        public RequestHoverText(string documentIdentifier, LinePosition position)
        {
            DocumentIdentifier = documentIdentifier;
            Position = position;
        }

        public static string MakeDataUriFromContents(string code)
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(code));
            return "data:text/plain;base64," + encoded;
        }
    }
}
