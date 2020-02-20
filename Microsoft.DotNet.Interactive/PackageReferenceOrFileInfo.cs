// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive
{
    public class PackageReferenceOrFileInfo
    {
        private readonly int _case;

        public PackageReferenceOrFileInfo(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            _case = 1;
        }

        public PackageReferenceOrFileInfo(PackageReference packageReference)
        {
            PackageReference = packageReference;
            _case = 2;
        }

        public FileInfo FileInfo { get; }

        public PackageReference PackageReference { get; }

        public object Value =>
            _case switch
            {
                1 => FileInfo,
                2 => PackageReference,
                _ => throw new InvalidOperationException()
            };

        public static implicit operator PackageReferenceOrFileInfo(FileInfo source) =>
            new PackageReferenceOrFileInfo(source);

        public static implicit operator PackageReferenceOrFileInfo(PackageReference source) =>
            new PackageReferenceOrFileInfo(source);
    }
}