// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.DotNet.Interactive.ExtensionLab;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class TabularDataResourceExtensions
    {
        public static SandDanceDataExplorer ExploreWithSandDance(this TabularDataResource source, bool immediateDisplay = true)
        {
            var explorer = new SandDanceDataExplorer(source);
            if (immediateDisplay)
            {
                KernelInvocationContext.Current?.Display(explorer, HtmlFormatter.MimeType);
            }
            return explorer;
        }

        public static NteractDataExplorer ExploreWithNteract(this TabularDataResource source, bool immediateDisplay = true)
        {
            var explorer = new NteractDataExplorer(source);
            if (immediateDisplay)
            {
                KernelInvocationContext.Current?.Display(explorer, HtmlFormatter.MimeType);
            }
            return explorer;
        }
    }
}