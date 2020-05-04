// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public interface ITag : IHtmlContent
    {
        /// <summary>
        ///   Gets HTML tag type.
        /// </summary>
        /// <value>The type of the tag.</value>
        string Name { get; }

        /// <summary>
        ///   Gets the HTML attributes to be rendered into the tag.
        /// </summary>
        /// <value>The HTML attributes.</value>
        HtmlAttributes HtmlAttributes { get; }
    }
}