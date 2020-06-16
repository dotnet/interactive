// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestCompletion : LanguageServiceCommandBase
    {
        public RequestCompletion(string code, LinePosition position, string targetKernelName = null)
            : base(code, position, targetKernelName)
        {
        }

        internal override LanguageServiceCommandBase WithCodeAndPosition(string code, LinePosition position)
        {
            return new RequestCompletion(code, position);
        }
    }
}
