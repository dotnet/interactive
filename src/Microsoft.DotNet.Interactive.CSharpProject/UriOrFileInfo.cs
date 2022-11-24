// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class UriOrFileInfo
{
    readonly FileInfo _fileInfo;
    readonly Uri _uri;
    public bool IsFile { get; }

    public string FileExtension { get; }

    public UriOrFileInfo(string value)
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
            IsFile = false;
        }
        else
        {
            _fileInfo = new FileInfo(value);
            IsFile = true;
            FileExtension = _fileInfo.Extension;
        }
    }

     

    public override string ToString()
    {
        return _fileInfo?.ToString() ?? _uri.ToString();
    }
}