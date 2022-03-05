// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.CSharpProject.Protocol;

namespace Microsoft.DotNet.Interactive.CSharpProject.MLS.Project
{
    public static class FileGenerator
    {
        public static File Create(string name, string content)
        {
            return new File(name, content);
        }
    }
}