// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

internal static class NotebookParserServerTestExtensions
{
    public static string AsUtf8String(this byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }
}