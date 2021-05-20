// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Assent;

using Microsoft.Data.Analysis;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class DataFrameKernelExtensionTests : IDisposable
    {

        private readonly Configuration _configuration;

        public DataFrameKernelExtensionTests()
        {
            _configuration = new Configuration()
                .SetInteractive(Debugger.IsAttached)
                .UsingExtension("json");
        }

        [Fact]
        public async Task it_registers_formatters()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new DataFrameKernelExtension();

            await kernelExtension.OnLoadAsync(kernel);

            var stream = @"id,name,color,deliciousness
1,apple,green,10
2,banana,yellow,11
3,cherry,red,9000".ToStream();

            var dataFrame = DataFrame.LoadCsv(stream);

            var formatted = dataFrame.ToDisplayString(TabularDataResourceFormatter.MimeType);

            this.Assent(formatted, _configuration);
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }
    }
}