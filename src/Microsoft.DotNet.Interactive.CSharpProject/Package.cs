// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public class Package
    {
        public bool IsWasmSupported { get; }

        public Package(bool isWasmSupported)
        {
            IsWasmSupported = isWasmSupported;
        }
    }
}