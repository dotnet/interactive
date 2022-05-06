// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

namespace System
{
    public static class DisplayExtensions
    {
        /// <summary>
        /// Formats an object using the <see cref="Formatter"/> into a string to be displayed. 
        /// </summary>
        /// <param name="value">The value to display.</param>
        /// <param name="mimeTypes">The MIME types.</param>
        /// <returns>An instance of <see cref="DisplayedValue"/> that can be used to later update the display.</returns>
        public static DisplayedValue Display(this object value,
            params string[] mimeTypes)
        {
            return KernelInvocationContext.Current.Display(value, mimeTypes);
        }

        public static DisplayedValue DisplayAs(this string value, string mimeType, params string[] additionalMimeTypes)
        {
             return KernelInvocationContext.Current.DisplayAs(value, mimeType, additionalMimeTypes);
        }
    }
}