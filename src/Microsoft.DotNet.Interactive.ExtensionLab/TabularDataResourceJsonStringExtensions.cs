// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.DotNet.Interactive.ExtensionLab;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class TabularDataResourceExtensions
    {
        public static DataExplorer ExploreWithSandDance(this TabularDataResource source)
        {
            var explorer = new DataExplorer(source);
            KernelInvocationContext.Current?.Display(explorer, HtmlFormatter.MimeType);
            return explorer;
        }
    }
    public static class TabularDataResourceJsonStringExtensions
    {
        public static void Explore(this TabularDataResourceJsonString source)
        {
            KernelInvocationContext.Current.Display(
                source,
                HtmlFormatter.MimeType);
        }
    }
}