// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public interface IRunResultFeature
    {
        string Name { get; }
        void Apply(FeatureContainer result);
    }
}
