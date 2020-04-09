// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    [XunitTestCaseDiscoverer("Microsoft.DotNet.Interactive.App.Tests.JupyterNotInstalledTestCaseDiscoverer", "Microsoft.DotNet.Interactive.App.Tests")]
    public class FactDependsOnJupyterNotInstalledAttribute : FactAttribute
    {
    }

    public class JupyterNotInstalledTestCaseDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink messageSink;

        public JupyterNotInstalledTestCaseDiscoverer(IMessageSink messageSink)
        {
            this.messageSink = messageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            if (!testMethod.TestClass.Class.Name.Contains("Integration") && !JupyterKernelSpecModule.IsOnPath && ! Jupyter.DefaultKernelPathExists)
            {
                yield return new XunitTestCase(
                    messageSink,
                    TestMethodDisplay.ClassAndMethod,
                    new TestMethodDisplayOptions(),
                    testMethod
                );
            }
        }
    }
}