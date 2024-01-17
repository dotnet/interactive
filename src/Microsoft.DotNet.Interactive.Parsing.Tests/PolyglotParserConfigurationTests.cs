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

    internal static PolyglotParserConfiguration GetDefaultConfiguration(string defaultKernelName = "") =>
        new(defaultKernelName)
        {
            KernelInfos =
            {
                [".NET"] = new KernelInfo(".NET", isComposite: true)
                {
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#!sql"),
                        new KernelSpecifierDirective("#!kql"),
                        new KernelSpecifierDirective("#!csharp"),
                        new KernelSpecifierDirective("#!fsharp"),
                        new KernelSpecifierDirective("#!pwsh"),
                        new KernelSpecifierDirective("#!html"),
                        new KernelSpecifierDirective("#!value")
                        {
                        },
                        new KernelSpecifierDirective("#!mermaid"),
                        new KernelSpecifierDirective("#!http"),
                        new KernelActionDirective("#!lsmagic"),
                        new KernelActionDirective("#!markdown"),
                        new KernelActionDirective("#!time"),
                        new KernelActionDirective("#!about"),
                        new KernelActionDirective("#!import")
                        {
                            new KernelDirectiveParameter("file")
                        },
                        new KernelActionDirective("#!connect")
                        {
                            new KernelActionDirective("stdio"),
                            new KernelActionDirective("signalr"),
                            new KernelActionDirective("jupyter"),
                        },
                        new KernelSpecifierDirective("#!javascript"),
                    }
                },
                ["csharp"] = new KernelInfo("csharp", new[] { "C#" })
                {
                    SupportedDirectives = new[]
                    {
                        new KernelActionDirective("#i"),
                        new KernelActionDirective("#r")
                    }
                },
                ["fsharp"] = new KernelInfo("fsharp", new[] { "F#" })
                {
                    SupportedDirectives = new[]
                    {
                        new KernelActionDirective("#i"),
                        new KernelActionDirective("#r")
                    }
                },
                ["pwsh"] = new KernelInfo("pwsh", new[] { "powershell" }),
            }
        };
}