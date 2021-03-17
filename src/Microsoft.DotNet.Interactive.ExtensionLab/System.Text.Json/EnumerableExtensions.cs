// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.ExtensionLab;
using Microsoft.DotNet.Interactive.Formatting;

namespace System.Text.Json
{
    public static class JsonExtensions
    {
        public static SandDanceDataExplorer ExploreWithSandDance(this JsonDocument source)
        {
            return source.ToTabularDataResource().ExploreWithSandDance();
        }

        public static NteractDataExplorer ExploreWithNteract(this JsonDocument source)
        {
            return source.ToTabularDataResource().ExploreWithNteract();
        }
        
        public static SandDanceDataExplorer ExploreWithSandDance(this JsonElement source)
        {
            return source.ToTabularDataResource().ExploreWithSandDance();
        }

        public static NteractDataExplorer ExploreWithNteract(this JsonElement source)
        {
            return source.ToTabularDataResource().ExploreWithNteract();
        }
    }
}