// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestHoverText : LanguageServiceCommandBase
    {
        public RequestHoverText(string code, LinePosition position)
            : base(code, position)
        {
        }

        internal RequestHoverText(string code, LinePosition position, IKernelCommand parent)
            : base(code, position, targetKernelName: null, parent: parent)
        {
        }

        internal override LanguageServiceCommandBase WithCodeAndPosition(string code, LinePosition position)
        {
            return new RequestHoverText(code, position);
        }
    }
}
