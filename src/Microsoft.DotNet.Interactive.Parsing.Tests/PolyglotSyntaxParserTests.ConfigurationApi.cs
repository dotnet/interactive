// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    [TestClass]
    public class ConfigurationApi
    {
        [TestMethod]
        public void Duplicate_subcommand_names_are_not_allowed_on_a_single_directive()
        {
            var directive = new KernelActionDirective("#!test");

            directive.Subcommands.Add(new("dupe"));

            var addAnother = () =>
                directive.Subcommands.Add(new("dupe"));

            addAnother.Should().Throw<ArgumentException>()
                      .Which.Message
                      .Should().Be("Directive already contains a subcommand named 'dupe'.");
        }

        [TestMethod]
        public void When_conflicting_parameter_names_are_configured_then_it_throws()
        {
            var directive = new KernelActionDirective("#!test");

            directive.Parameters.Add(new("--dupe"));

            var addAnother = () =>
                directive.Parameters.Add(new("--dupe"));

            addAnother.Should().Throw<ArgumentException>()
                      .Which.Message
                      .Should().Be("Directive already contains a parameter named '--dupe'.");
            
        }

        [TestMethod]
        public void Multiple_implicit_property_names_are_not_allowed_on_a_single_directive()
        {
            var directive = new KernelActionDirective("#!test");
            directive.Parameters.Add(new("--opt1")
            {
                AllowImplicitName = true
            });

            var addAnother = () =>
                directive.Parameters.Add(new("--opt2")
                {
                    AllowImplicitName = true
                });

            addAnother.Should().Throw<ArgumentException>()
                      .Which.Message
                      .Should().Be($"Only one parameter on a directive can have {nameof(KernelDirectiveParameter.AllowImplicitName)} set to true.");
        }
    }
}