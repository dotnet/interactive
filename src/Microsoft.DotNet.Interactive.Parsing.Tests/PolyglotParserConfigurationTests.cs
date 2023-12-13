// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public class PolyglotParserConfigurationTests
{
    [Fact]
    public void Kernel_name_magic_is_recognized_as_kernel_chooser()
    {
        var config = GetDefaultConfiguration();

        config.IsKernelSelectorDirective("#!csharp").Should().BeTrue();
    }

    [Fact]
    public void Kernel_alias_magic_is_recognized_as_kernel_chooser()
    {
        var config = GetDefaultConfiguration();

        config.IsKernelSelectorDirective("#!F#").Should().BeTrue();
    }

    internal static PolyglotParserConfiguration GetDefaultConfiguration() =>
        new()
        {
            KernelInfos =
            {
                [".NET"] = new KernelInfo(".NET", isComposite: true)
                {
                    SupportedDirectives = new[]
                    {
                        new KernelDirectiveInfo("#!sql", true),
                        new("#!kql", true),
                        new("#!csharp", true),
                        new("#!fsharp", true),
                        new("#!pwsh", true),
                        new("#!html", true),
                        new("#!value", true),
                        new("#!mermaid", true),
                        new("#!http", true),
                        new("#!lsmagic", false),
                        new("#!markdown", false),
                        new("#!time", false),
                        new("#!about", false),
                        new("#!import", false),
                        new("#!connect", false),
                        new("#!javascript", true),
                    }
                },
                ["csharp"] = new KernelInfo("csharp", new[] { "C#" })
                {
                    SupportedDirectives = new[]
                    {
                        new KernelDirectiveInfo("#i", false),
                        new("#r", false)
                    }
                },
                ["fsharp"] = new KernelInfo("fsharp", new[] { "F#" })
                {
                    SupportedDirectives = new[]
                    {
                        new KernelDirectiveInfo("#i", false),
                        new("#r", false)
                    }
                },
                ["pwsh"] = new KernelInfo("pwsh", new[] { "powershell" }),
            }
        };
}