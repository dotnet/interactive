// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.ExtensionLab;
using Microsoft.DotNet.Interactive.Formatting;

namespace System.Text.Json
{
    public static class JsonExtensions
    {
        public static SandDanceDataExplorer ExploreWithSandDance(this JsonDocument source)
        {
            var explorer = new SandDanceDataExplorer(source.ToTabularDataResource());
            KernelInvocationContext.Current?.Display(explorer, HtmlFormatter.MimeType);
            return explorer;
        }

        public static NteractDataExplorer ExploreWithNteract(this JsonDocument source)
        {
            var explorer = new NteractDataExplorer(source.ToTabularDataResource());
            KernelInvocationContext.Current?.Display(explorer, HtmlFormatter.MimeType);
            return explorer;
        }
        
        public static SandDanceDataExplorer ExploreWithSandDance(this JsonElement source)
        {
            var explorer = new SandDanceDataExplorer(source.ToTabularDataResource());
            KernelInvocationContext.Current?.Display(explorer, HtmlFormatter.MimeType);
            return explorer;
        }

        public static NteractDataExplorer ExploreWithNteract(this JsonElement source)
        {
            var explorer = new NteractDataExplorer(source.ToTabularDataResource());
            KernelInvocationContext.Current?.Display(explorer, HtmlFormatter.MimeType);
            return explorer;
        }
    }
}