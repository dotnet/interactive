// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class JsonString : HtmlString
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonString"/> class.
        /// </summary>
        /// <param name="json">The json.</param>
        public JsonString(string json) : base(json)
        {
        }
    }
}