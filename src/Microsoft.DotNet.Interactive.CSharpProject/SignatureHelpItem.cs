// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    public class SignatureHelpItem
    {
        // FIX: (SignatureHelpItem) use SignatureInformation instead
        public string Name { get; set; }

        public string Label { get; set; }

        public string Documentation { get; set; }

        public IEnumerable<SignatureHelpParameter> Parameters { get; set; }
    }
}
