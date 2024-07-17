// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Journey;

public class StartLesson : KernelDirectiveCommand
{
    public StartLesson(FileInfo fromFile = null, Uri fromUrl = null)
    {
        FromFile = fromFile;
        FromUrl = fromUrl;
    }

    public FileInfo FromFile { get; }

    public Uri FromUrl { get; }

    public override IEnumerable<string> GetValidationErrors()
    {
        if (FromFile is not null && 
            FromUrl is not null)
        {
            yield return "The --from-url and --from-file options cannot be used together";
        }
    }
}