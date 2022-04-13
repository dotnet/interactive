// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class TestPackageInitializer : PackageInitializer
    {
        public int InitializeCount { get; private set; }

        public TestPackageInitializer(
            string template,
            string projectName,
            string language = null,
            Func<DirectoryInfo, Task> afterCreate = null) :
            base(template, projectName, language, afterCreate)
        {
        }

        public override Task InitializeAsync(DirectoryInfo directory)
        {
            InitializeCount++;
            return base.InitializeAsync(directory);
        }
    }
}
