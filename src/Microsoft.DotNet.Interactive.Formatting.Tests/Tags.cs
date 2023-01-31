// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public static class Tags
{
    public const string PlainTextBegin = "<div class=\"dni-plaintext\"><pre>";
    public const string PlainTextEnd = "</pre></div>";  
    
    public const string SummaryTextBegin = "<summary><span class=\"dni-code-hint\"><code>";
    public const string SummaryTextEnd = "</code></span></summary>";

    public static readonly string DefaultStyles;


    static Tags()
    {
        var formatter = new HtmlFormatter<string>((_, context) => context.RequireDefaultStyles());

        using var writer = new StringWriter();

        formatter.Format("", writer);

        DefaultStyles = writer.ToString();
    }
}