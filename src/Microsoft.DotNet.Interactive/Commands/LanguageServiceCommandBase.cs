// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Commands
{
    public abstract class LanguageServiceCommandBase : KernelCommandBase
    {
        public string Code { get; protected set; }
        public LinePosition Position { get; protected set; }

        protected LanguageServiceCommandBase(string code, LinePosition position, string targetKernelName = null, IKernelCommand parent = null)
            : base(targetKernelName, parent)
        {
            Code = code;
            Position = position;
        }

        internal abstract LanguageServiceCommandBase WithCodeAndPosition(string code, LinePosition position);
    }
}
