// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.DotNet.Interactive.Utility
{
    internal static class KernelUriExtensions
    {
        public static string GetLocalKernelName(this string kernelUriString)
        {
            if (string.IsNullOrEmpty(kernelUriString))
            {
                return kernelUriString;
            }

            return KernelUri.Parse(kernelUriString).GetLocalKernelName();
        }

        public static string GetLocalKernelName(this KernelUri uri)
        {
            return uri.Parts[0];
        }

        public static string GetRemoteKernelName(this KernelUri uri)
        {
            if (uri.Parts.Length <= 1)
            {
                return null;
            }

            return string.Join("/", uri.Parts.Skip(1));
        }
    }
}
