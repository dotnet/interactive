// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestValueInfos : KernelCommand
{
    public RequestValueInfos(
        string targetKernelName = null, 
        string mimeType = PlainTextSummaryFormatter.MimeType) : base(targetKernelName)
    {
        MimeType = mimeType;
    }

    /// <summary>
    /// The MIME type to be used for the format of the <see cref="KernelValueInfo.FormattedValue"/> instances to be returned.
    /// </summary>
    public string MimeType { get; }
}