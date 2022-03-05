// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public static class RunResultExtensions
    {
        public static T GetFeature<T>(this FeatureContainer result) 
            where T : class, IRunResultFeature
        {
            if (result.Features.TryGetValue(typeof(T).Name, out var feature))
            {
                return feature as T;
            }
            else
            {
                return null;
            }
        }
    }
}