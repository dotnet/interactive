// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Parsing;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Commands
{
    public abstract class SplittableCommand : KernelCommand
    {
        public SplittableCommand(
            string code,
            string targetKernelName = null) : base(targetKernelName)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }

        internal SplittableCommand(
            LanguageNode languageNode,
            KernelCommand parent = null)
            : base(languageNode.Language, parent)
        {
            Code = languageNode.Text;
            LanguageNode = languageNode;
        }

        public string Code { get; }

        [JsonIgnore]
        public LanguageNode LanguageNode { get; }
    }
}
