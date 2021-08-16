// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents
{
    public class DisplayElement : InteractiveDocumentOutputElement
    {
        public IDictionary<string, object> Data { get; }

        public DisplayElement(IDictionary<string, object> data)
        {
            Data = data;
        }
    }
}