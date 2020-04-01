﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing
{
    [DebuggerStepThrough]
    public class KernelDirectiveNode : DirectiveNode
    {
        internal KernelDirectiveNode(
            DirectiveToken directiveToken, 
            SourceText sourceText) : base(directiveToken, sourceText)
        {
            KernelName = directiveToken.DirectiveName;
        }

        public string KernelName { get; set; }
    }
}