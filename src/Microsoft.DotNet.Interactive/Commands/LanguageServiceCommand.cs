// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands
{
    public abstract class LanguageServiceCommand : KernelCommand
    {
        protected LanguageServiceCommand(
            string code,
            LinePosition linePosition,
            string targetKernelName = null,
            KernelCommand parent = null)
            : base(targetKernelName, parent)
        {
            Code = code;
            LinePosition = linePosition;
        }
        
        protected LanguageServiceCommand(
            LanguageNode languageNode,
            LinePosition linePosition,
            KernelCommand parent = null)
            : base(languageNode.Language, parent)
        {
            Code = languageNode.Text;
            LanguageNode = languageNode;
            LinePosition = linePosition;
        }

        public string Code { get; protected set; }

        public LinePosition LinePosition { get; protected set; }

        internal abstract LanguageServiceCommand With(
            LanguageNode languageNode, 
            LinePosition position);

        internal LanguageNode LanguageNode { get; }
    }
}