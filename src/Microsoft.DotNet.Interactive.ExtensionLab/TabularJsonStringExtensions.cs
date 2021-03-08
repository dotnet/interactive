// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.DotNet.Interactive.ExtensionLab;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class TabularJsonStringExtensions
    {
        public static DataExplorer ExploreWithSandDance(this TabularJsonString source)
        {
            var explorer = new DataExplorer(source);
            return explorer;
        }

        public static void Explore(this TabularJsonString source)
        {
            KernelInvocationContext.Current.Display(
                source,
                HtmlFormatter.MimeType);
        }
    }
}