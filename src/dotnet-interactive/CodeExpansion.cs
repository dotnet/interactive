// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.App;

public class CodeExpansion
{
    public CodeExpansion(
        IReadOnlyList<CodeExpansionSubmission> content,
        CodeExpansionInfo info)
    {
        Content = content;
        Info = info;
    }

    public CodeExpansionInfo Info { get; }

    public IReadOnlyList<CodeExpansionSubmission> Content { get; }

    public static class CodeExpansionKind
    {
        public const string RecentConnection = nameof(RecentConnection);
        public const string KernelSpecConnection = nameof(KernelSpecConnection);
        public const string DataConnection = nameof(DataConnection);
    }
}