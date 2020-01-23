// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

    using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public partial class MagicCommandTests
    {
        public class about
        {
            [Fact]
            public async Task it_shows_the_product_name_and_version_information()
            {
                using var kernel = new CompositeKernel()
                    .UseAbout();

                using var events = kernel.KernelEvents.ToSubscribedList();

                await kernel.SubmitCodeAsync("#!about");

                events.Should()
                      .ContainSingle<DisplayedValueProduced>()
                      .Which
                      .FormattedValues
                      .Should()
                      .ContainSingle(v => v.MimeType == "text/html")
                      .Which
                      .Value
                      .As<string>()
                      .Should()
                      .ContainAll(
                          ".NET Interactive",
                          "Version", 
                          "https://github.com/dotnet/interactive");
            }
        }
    }
}