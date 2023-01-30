// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown;

internal abstract class HtmlStyleAttribute
{
    public override string ToString()
    {
        return StyleAttributeString().ToString();
    }

    protected abstract IHtmlContent StyleAttributeString();
}