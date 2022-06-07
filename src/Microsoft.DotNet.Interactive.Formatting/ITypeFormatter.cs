// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Formatting;

public interface ITypeFormatter
{
    string MimeType { get; }

    Type Type { get; }

    bool Format(object instance, FormatContext context);
}