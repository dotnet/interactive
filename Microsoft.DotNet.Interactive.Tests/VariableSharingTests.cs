// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class VariableSharingTests
    {
        [Theory]
        [InlineData(
            "#!fsharp",
            "let x = 123",
            "#!csharp",
            "(GetKernel(\"fsharp\") as Microsoft.DotNet.Interactive.LanguageKernel).TryGetVariable(\"x\", out int x);\nx")]
        [InlineData(
            "#!fsharp",
            "let x = 123",
            "#!csharp",
            "#!share --from fsharp x\nfsharp.TryGetVariable(\"x\", out int x);\nx", Skip = "WIP")]
        public async Task Variables_can_be_read_from_other_kernels(
            string fromLanguage,
            string codeToWrite,
            string toLanguage,
            string codeToRead)
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel()
                    .UseKernelHelpers()
                    .UseDotNetVariableSharing(),
                new FSharpKernel()
                    .UseKernelHelpers()
                    .UseDotNetVariableSharing(),
                new PowerShellKernel()
                    .UseDotNetVariableSharing()
            }.LogEventsToPocketLogger();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"{fromLanguage}\n{codeToWrite}");

            await kernel.SubmitCodeAsync($"{toLanguage}\n{codeToRead}");

            events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .Should()
                  .Be(123);
        }

        [Fact(Skip = "WIP")]
        public void Internal_types_are_shared_as_their_most_public_supertype()
        {
            throw new NotImplementedException("test not written");
        }
    }
}