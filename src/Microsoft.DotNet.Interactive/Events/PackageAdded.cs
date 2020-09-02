// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class PackageAdded : KernelEvent
    {
        public PackageAdded(
            ResolvedPackageReference packageReference,
            AddPackage command = null) : base(command)
        {
            PackageReference = packageReference ?? throw new ArgumentNullException(nameof(packageReference));
        }

        public ResolvedPackageReference PackageReference { get; }

        public override string ToString()
        {
            return $"{base.ToString()}: {PackageReference}";
        }
    }
}