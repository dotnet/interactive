// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.ValueSharing;

internal class ShareDirectiveCommand : KernelCommand
{
    public string Name { get; set; }
    public string As { get; set; }
    public string From { get; set; }

    public string MimeType { get; set; }

    public static async Task HandleAsync(ShareDirectiveCommand arg1, KernelInvocationContext arg2)
    {
    }
}