// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static void Explore<T>(this IEnumerable<T> source)
        {
            KernelInvocationContext.Current.Display(
                source.ToTabularJsonString(),
                HtmlFormatter.MimeType);
        }
    }
}