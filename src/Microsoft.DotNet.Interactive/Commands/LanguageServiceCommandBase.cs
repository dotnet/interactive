// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands
{
    public abstract class LanguageServiceCommandBase : KernelCommandBase
    {
        protected LanguageServiceCommandBase(
            string code,
            LinePosition position,
            string targetKernelName = null,
            IKernelCommand parent = null)
            : base(targetKernelName, parent)
        {
            Code = code;
            Position = position;
        }
        
        protected LanguageServiceCommandBase(
            LanguageNode languageNode,
            LinePosition position,
            IKernelCommand parent = null)
            : base(languageNode.Language, parent)
        {
            Code = languageNode.Text;
            LanguageNode = languageNode;
            Position = position;
        }

        public string Code { get; protected set; }

        public LinePosition Position { get; protected set; }

        internal abstract LanguageServiceCommandBase With(
            LanguageNode languageNode, 
            LinePosition position);

        internal LanguageNode LanguageNode { get; }
    }
}