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
            LinePosition position, 
            string targetKernelName = null)
            : base(code, position, targetKernelName)
        {
        }

        internal RequestCompletion(
            LanguageNode languageNode, 
            LinePosition position, 
            KernelCommand parent = null) 
            : base(languageNode, position, parent)
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
