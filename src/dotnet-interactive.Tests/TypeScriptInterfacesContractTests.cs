﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.CompilerServices;
using Assent;
using FluentAssertions;
using Microsoft.DotNet.Interactive.InterfaceGen.App;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class TypeScriptInterfacesContractTests
    {
        private string GetTypeScriptContractFullPath(string subPath, [CallerFilePath] string thisDir = null)
        {
            var sourceRoot = Path.Combine(Path.GetDirectoryName(thisDir), "..", "..");
            var fullPath = Path.Combine(sourceRoot, subPath);
            return fullPath;
        }

        private void CheckTypeScriptInterfaceFile(string interfaceFileSubPath)
        {
            var contractFile = new FileInfo(GetTypeScriptContractFullPath(interfaceFileSubPath));
            contractFile.Exists.Should().BeTrue(
                $"The Typescript contract file {interfaceFileSubPath} does not exist. Please run the `src/interface-generator` tool with option --out-file {contractFile.FullName}.");

            var actual = File.ReadAllText(contractFile.FullName);
            var expected = InterfaceGenerator.Generate();

            var compareResult = new DefaultStringComparer(true).Compare(actual, expected);

            compareResult
                .Error
                .Should()
                .BeNullOrEmpty(
                    because:
                    $@"{contractFile.Name} should match the checked-in version.

If the contract change is deliberate, then the TypeScript contracts file '{interfaceFileSubPath}' needs to be regenerated.

Please run the following:

dotnet run -p src/interface-generator -- --out-file {contractFile.FullName}

");
        }

        [Fact]
        public void vscode_generated_TypeScript_interfaces_file_has_known_shape()
        {
            CheckTypeScriptInterfaceFile("src/dotnet-interactive-vscode/src/contracts.ts");
        }

        [Fact]
        public void http_generated_TypeScript_interfaces_file_has_known_shape()
        {
            CheckTypeScriptInterfaceFile("src/Microsoft.DotNet.Interactive.Js/src/dotnet-interactive/contracts.ts");
        }
    }
}