// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class KernelHttpApiTests
    {

        [Fact]
        public async Task can_get_variable_from_kernel ()
        {
            using var server = await InProcessTestServer<Startup>.StartServer("http --default-kernel csharp");
            var kernel = server.Kernel;
            var client = server.Client;

            await kernel.SendAsync(new SubmitCode("var a = 123;", "csharp"));

            var response = await client.GetAsync("/variables/csharp/a");

            var responseContent = await response.Content.ReadAsStringAsync();

            var value = JToken.Parse(responseContent).Value<int>();

            // read
            value.Should().Be(123);

        }
    }
}
