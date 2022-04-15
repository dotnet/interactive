// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    public class OutputBlockAnnotations : CodeFenceAnnotations
    {
        public OutputBlockAnnotations(
            ParseResult parseResult = null,
            string session = null)
            : base(parseResult, session)
        {
        }
    }
}