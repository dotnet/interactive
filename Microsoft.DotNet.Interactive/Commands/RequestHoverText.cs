// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Microsoft.DotNet.Interactive.LanguageService;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestHoverText : LanguageServiceCommandBase
    {
        public string DocumentIdentifier { get; }
        public Position Position { get; }

        public RequestHoverText(string documentIdentifier, Position position)
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
