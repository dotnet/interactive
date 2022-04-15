// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.WorkspaceFeatures
{
    public class UnitTestRun : IRunResultFeature
    {
        public UnitTestRun(IEnumerable<UnitTestResult> results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results)) ;
        }

        public IEnumerable<UnitTestResult> Results { get; }

        public string Name => nameof(UnitTestRun);

        public void Apply(FeatureContainer result)
        {
            result.AddProperty("testResults", Results);
        }
    }
}