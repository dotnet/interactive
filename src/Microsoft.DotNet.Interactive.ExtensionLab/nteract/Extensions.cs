// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.ExtensionLab.nteract
{
    public static class Extensions
    {
        public static void Explore<T>(this IEnumerable<T> source)
        {
            KernelInvocationContext.Current.Display(
                source.ToTabularJsonString(),
                HtmlFormatter.MimeType);
        }
    }
}
