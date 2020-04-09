// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    [XunitTestCaseDiscoverer("Microsoft.DotNet.Interactive.App.Tests.JupyterInstalledTestCaseDiscoverer", "Microsoft.DotNet.Interactive.App.Tests")]
    public class FactDependsOnJupyterOnPathAttribute : FactAttribute
    {
    }

    public class JupyterInstalledTestCaseDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink messageSink;

        public JupyterInstalledTestCaseDiscoverer(IMessageSink messageSink)
        {
            this.messageSink = messageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            if (testMethod.TestClass.Class.Name.Contains("Integration") && !JupyterKernelSpecModule.Exists())
            {
                yield break;
            }

            yield return new XunitTestCase(
                messageSink,
                TestMethodDisplay.ClassAndMethod,
                new TestMethodDisplayOptions(),
                testMethod
            );
        }
    }
}