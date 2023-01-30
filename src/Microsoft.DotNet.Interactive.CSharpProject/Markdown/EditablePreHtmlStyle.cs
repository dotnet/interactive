// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown;

internal class EditablePreHtmlStyle : HtmlStyleAttribute
{
    private readonly string _height;

    public EditablePreHtmlStyle(string height)
    {
        _height = height;
    }

    protected override IHtmlContent StyleAttributeString()
    {
        return new HtmlString(  $@"style=""border:none; height:{_height}""");
    }
}