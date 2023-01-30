// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown;

public class DefaultCodeBlockAnnotations : IDefaultCodeBlockAnnotations
{
    public string Package { get; set; }

    public string PackageVersion { get; set; }
}