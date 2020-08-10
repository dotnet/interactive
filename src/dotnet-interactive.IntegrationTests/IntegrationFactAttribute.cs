// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests
{
    /// <summary>
    /// Signifies that a unit test should only be run during the integration test leg and not as part of a normal test
    /// run or during the regular dev loop.
    /// 
    /// To run locally, [<see cref="IntegrationFactAttribute"/>] will need to be replaced with [<see cref="FactAttribute"/>].
    /// </summary>
    public class IntegrationFactAttribute : FactAttribute
    {
        public IntegrationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INTEGRATION_TEST_RUN")))
            {
                Skip = "Only run as an integration test";
            }
        }
    }
}
