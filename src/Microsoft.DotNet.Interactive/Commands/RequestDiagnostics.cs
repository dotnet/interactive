// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestDiagnostics : KernelCommand
    {
        public RequestDiagnostics(
            string code,
            string targetKernelName = null) : base(targetKernelName)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }

        internal RequestDiagnostics(
            LanguageNode languageNode,
            KernelCommand parent = null)
            : base(languageNode.Language, parent)
        {
            Code = languageNode.Text;
            LanguageNode = languageNode;

            if (languageNode is ActionDirectiveNode actionDirectiveNode)
            {
                TargetKernelName = actionDirectiveNode.ParentLanguage;
            }
        }

        public string Code { get; }

        internal LanguageNode LanguageNode { get; }
    }
}