// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Tests;

namespace Microsoft.DotNet.Interactive.App.Tests;

[TestClass]
public class FileProviderTests : LanguageKernelTestBase
{
    public FileProviderTests(TestContext output) : base(output)
    {
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public void It_loads_content_from_root_provider(Language language)
    {
        var kernel = CreateKernel(language);
        var provider = new FileProvider(kernel, typeof(Program).Assembly);

        var file = provider.GetFileInfo("resources/dotnet-interactive.js");

        file.Should()
            .NotBeNull();
    }
}