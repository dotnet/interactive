// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public partial class MagicCommandTests
    {
        [Collection("Do not parallelize")]
        public class about
        {
            [Fact]
            public async Task it_shows_the_product_name_and_version_information()
            {
                using var kernel = new CompositeKernel()
                    .UseAboutMagicCommand();

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