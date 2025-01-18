// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Microsoft.DotNet.Interactive.App;

public record CodeExpansionInfo(
    string Name,
    string Kind,
    string Description = null)
{
    public static class CodeExpansionKind
    {
        public const string RecentConnection = nameof(RecentConnection);
        public const string KernelSpecConnection = nameof(KernelSpecConnection);
        public const string WellKnownConnection = nameof(WellKnownConnection);
    }
}