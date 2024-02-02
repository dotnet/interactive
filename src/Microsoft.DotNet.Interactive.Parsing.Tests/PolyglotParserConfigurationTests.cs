// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Directives;
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
                new KernelInfo(".NET", isComposite: true)
                {
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#!sql"),
                        new KernelSpecifierDirective("#!kql"),
                        new KernelSpecifierDirective("#!csharp"),
                        new KernelSpecifierDirective("#!fsharp"),
                        new KernelSpecifierDirective("#!pwsh"),
                        new KernelSpecifierDirective("#!html"),
                        new KernelSpecifierDirective("#!value"),
                        new KernelSpecifierDirective("#!mermaid"),
                        new KernelSpecifierDirective("#!http"),
                        new KernelActionDirective("#!lsmagic"),
                        new KernelActionDirective("#!markdown"),
                        new KernelActionDirective("#!time"),
                        new KernelActionDirective("#!about"),
                        new KernelActionDirective("#!import")
                        {
                            Parameters =
                            {
                                new KernelDirectiveParameter("file")
                            }
                        },
                        new KernelActionDirective("#!connect")
                        {
                            Parameters =
                            {
                                new KernelDirectiveParameter("--kernel-name")
                            },
                            Subcommands =
                            {
                                new KernelActionDirective("stdio"),
                                new KernelActionDirective("signalr"),
                                new KernelActionDirective("jupyter"),
                                new KernelActionDirective("mssql")
                                {
                                    Parameters =
                                    {
                                        new KernelDirectiveParameter("--connection-string"),
                                    },
                                    DeserializeAs = typeof(ConnectMsSql)
                                },
                            }
                        },
                        new KernelSpecifierDirective("#!javascript"),
                    }
                },
                new KernelInfo("csharp", new[] { "C#" })
                {
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#i"),
                        new KernelActionDirective("#r")
                    }
                },
                new KernelInfo("fsharp", new[] { "F#" })
                {
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#i"),
                        new KernelActionDirective("#r")
                    }
                },
                new KernelInfo("pwsh", new[] { "powershell" }),
            }
        };
}

internal class KernelCommand
{
    protected KernelCommand(string targetKernelName = null)
    {
        TargetKernelName = targetKernelName;
    }

    public string TargetKernelName { get; internal set; }

    public Uri OriginUri { get; set; }

    public Uri DestinationUri { get; set; }
}

internal class ConnectMsSql : KernelCommand
{
    public string ConnectionString { get; set; }
}