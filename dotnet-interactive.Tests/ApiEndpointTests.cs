// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class SignalRTests
    {
        [Fact]
        public async Task Can_access_kernel_variables()
        {
            using var kernel = new CSharpKernel();
            await kernel.SendAsync(new SubmitCode("var a = 123;"));

          //  var hub = new KernelHub(() => kernel);

            throw new NotImplementedException();
        }
    }
}
