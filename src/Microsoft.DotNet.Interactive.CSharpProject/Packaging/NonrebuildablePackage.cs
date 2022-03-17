// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reactive.Concurrency;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public class NonrebuildablePackage : Package
    {

        public NonrebuildablePackage(string name = null, IPackageInitializer initializer = null, DirectoryInfo directory = null, IScheduler buildThrottleScheduler = null) 
            : base(name, initializer, directory, buildThrottleScheduler)
        {

        }
        
    }
}
