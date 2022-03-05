// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Clockwise;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public interface IPackageDiscoveryStrategy
    {
        Task<PackageBuilder> Locate(PackageDescriptor packageInfo, Budget budget = null);
    }
}