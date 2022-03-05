// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    internal class HiddenPreHtmlStyle : HtmlStyleAttribute
    {
        protected override IHtmlContent StyleAttributeString()
        {
            return new HtmlString( @"style=""border:none; margin:0px; padding:0px; visibility:hidden; display: none;""");
        }
    }
}