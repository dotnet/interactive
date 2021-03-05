// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.DotNet.Interactive.ExtensionLab;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class TabularJsonStringExtensions
    {
        public static SandDanceExplorer ExploreWithSandDance(this TabularJsonString source)
        {
            var explorer = new SandDanceExplorer();
            explorer.LoadData(source);
            return explorer;
        }
    }
}