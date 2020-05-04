// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class AddPackage : KernelCommandBase
    {
        public AddPackage(PackageReference packageReference)
        {
            PackageReference = packageReference ?? throw new ArgumentNullException(nameof(packageReference));
        }

        public PackageReference PackageReference { get; }
    }
}