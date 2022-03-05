// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public class PackageSource
    {
        private readonly DirectoryInfo _directory;
        private readonly Uri _uri;

        public PackageSource(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // Uri.IsWellFormed will return false for path-like strings:
            // (https://docs.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring?view=netcore-2.2)
            if (Uri.IsWellFormedUriString(value, UriKind.Absolute) &&
                Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && uri?.Scheme != null)
            {
                _uri = uri;
            }
            else
            {
                _directory = new DirectoryInfo(value);
            }
        }

        public override string ToString()
        {
            return _directory?.ToString() ?? _uri.ToString();
        }
    }
}