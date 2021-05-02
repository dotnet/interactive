// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Parsing;
using System.Threading.Tasks;
using Xunit;
using System;
using System.CommandLine;

namespace Microsoft.DotNet.Interactive.Tests.Parsing
{
    public class DirectiveParserTests
    {
        private static Command GetRDirective()
        {
            var members = typeof(KernelSupportsNugetExtensions).GetMembers(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var allRs = typeof(KernelSupportsNugetExtensions).GetMethod("r", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) ?? throw new Exception("Cannot locate r or it is ambiguous");
            var res = allRs.Invoke(null, Array.Empty<object>());
            if (res is Command command)
                return command;
            else
                throw new Exception("Returned object is not command");
        }

        [Fact]
        public void Test1()
        {
            var rootCommand = new RootCommand();
            rootCommand.Add(GetRDirective());
            var directiveParser = new DirectiveParser(rootCommand);
            var res = directiveParser.Parse("#r \"nuget:Duck\" //quack");
            res.Errors.Should().BeEmpty();
        }
    }
}
