// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestCompletion : LanguageServiceCommand
    {
        public RequestCompletion(
            string code, 
            LinePosition linePosition, 
            string targetKernelName = null)
            : base(code, linePosition, targetKernelName)
        {
        }

        internal RequestCompletion(
            LanguageNode languageNode, 
            LinePosition linePosition, 
            KernelCommand parent = null) 
            : base(languageNode, linePosition, parent)
        {
        }

        internal override LanguageServiceCommand With(
            LanguageNode languageNode, 
            LinePosition position)
        {
            return new RequestCompletion(languageNode, position, Parent);
        }
    }
}
