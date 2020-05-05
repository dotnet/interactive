// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class TypeScriptInterfacesContractTests
    {

        private string GetTypeScriptContractContents(string subPath, [CallerFilePath] string thisDir = null)
        {
            var sourceRoot = Path.Combine(Path.GetDirectoryName(thisDir), "..", "..");
            var fullPath = Path.Combine(sourceRoot, subPath);
            var contents = File.ReadAllText(fullPath);
            return contents;
        }

        private void CheckTypeScriptInterfaceFile(string interfaceFileSubPath)
        {
            var actual = GetTypeScriptContractContents(interfaceFileSubPath);
            var expected = InterfaceGenerator.Generate();
            actual.Should()
                .Be(expected, $"The contents of the TypeScript contracts file '{interfaceFileSubPath}' needs to be updated.  Please re-run the `src/interface-generator` tool.");
        }

        [Fact]
        public void http_generated_TypeScript_interfaces_file_has_known_shape()
        {
            CheckTypeScriptInterfaceFile("src/Microsoft.DotNet.Interactive.Js/src/dotnet-interactive/contracts.ts");
        }

        [Fact]
        public void vscode_generated_TypeScript_interfaces_file_has_known_shape()
        {
            CheckTypeScriptInterfaceFile("src/dotnet-interactive-vscode/src/contracts.ts");
        }
    }
}
