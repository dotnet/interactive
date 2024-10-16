// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Formatting;
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

    internal static PolyglotParserConfiguration GetDefaultConfiguration(string defaultKernelName = "csharp") =>
        new(defaultKernelName)
        {
            KernelInfos =
            {
                new KernelInfo(".NET", isComposite: true)
                {
                    SupportedDirectives =
                    {
                        new KernelSpecifierDirective("#!kql", "kql"),
                        new KernelSpecifierDirective("#!csharp", "csharp"),
                        new KernelSpecifierDirective("#!fsharp", "fsharp"),
                        new KernelSpecifierDirective("#!pwsh", "pwsh"),
                        new KernelSpecifierDirective("#!html", "html"),
                        new KernelSpecifierDirective("#!value", "value"),
                        new KernelSpecifierDirective("#!mermaid", "mermaid"),
                        new KernelSpecifierDirective("#!http", "http"),
                        
                        new KernelActionDirective("#!sql"),
                        new KernelActionDirective("#!lsmagic"),
                        new KernelActionDirective("#!markdown"),
                        new KernelActionDirective("#!time"),
                        new KernelActionDirective("#!about"),
                        new KernelActionDirective("#!import")
                        {
                            Parameters =
                            {
                                new KernelDirectiveParameter("file")
                                {
                                    AllowImplicitName = true,
                                    Required = true,
                                    TypeHint = "file"
                                }
                            }
                        },
                        new KernelActionDirective("#!connect")
                        {
                            Subcommands =
                            {
                                new KernelActionDirective("named-pipe")
                                {
                                    Parameters =
                                    {
                                        new("--pipe-name")
                                        {
                                            Required = true
                                        },
                                        new("--kernel-name")
                                        {
                                            Required = true
                                        }
                                    }
                                },
                                new KernelActionDirective("stdio")
                                {
                                    Parameters =
                                    {
                                        new("--working-directory"),
                                        new("--command"),
                                        new("--kernel-host"),
                                        new("--kernel-name")
                                        {
                                            Required = true
                                        }
                                    }
                                },
                                new KernelActionDirective("signalr")
                                {
                                    Parameters =
                                    {
                                        new("--hub-url")
                                        {
                                            Required = true
                                        },
                                        new("--kernel-name")
                                        {
                                            Required = true
                                        }
                                    }
                                },
                                new KernelActionDirective("jupyter")
                                {
                                    Parameters =
                                    {
                                        new("--kernel-name")
                                        {
                                            Required = true
                                        },
                                        new("--kernel-spec", "The kernel spec to connect to")
                                        {
                                            Required = true
                                        },
                                        new("--init-script", "Script to run on kernel initialization")
                                        {
                                            TypeHint = "file"
                                        },
                                        new("--conda-env"),
                                        new("--url"),
                                        new("--bearer")
                                        {
                                            Flag = true
                                        }
                                    }
                                },
                                new KernelActionDirective("mssql")
                                {
                                    Parameters =
                                    {
                                        new KernelDirectiveParameter("--connection-string")
                                        {
                                            AllowImplicitName = true,
                                            Required = true,
                                            TypeHint = "connectionstring-mssql"
                                        },
                                        new KernelDirectiveParameter("--create-dbcontext")
                                        {
                                            Flag = true
                                        },
                                        new("--kernel-name")
                                        {
                                            Required = true
                                        }
                                    },
                                    KernelCommandType = typeof(ConnectMsSql)
                                }
                            }
                        },
                        new KernelSpecifierDirective("#!javascript", "javascript")
                    }
                },
                new KernelInfo("csharp", ["C#"])
                {
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#i"),
                        new KernelActionDirective("#r"),
                        new KernelActionDirective("#!lsmagic"),
                        new KernelActionDirective("#!who"),
                        new KernelActionDirective("#!whos"),
                        new KernelActionDirective("#!set")
                        {
                            Parameters =
                            {
                                new("--name")
                                {
                                    Required = true
                                },
                                new("--value")
                                {
                                    Required = true
                                }, 
                            },
                            KernelCommandType = typeof(SendValue)
                        }
                    }
                },
                new KernelInfo("fsharp", ["F#"])
                {
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#i"),
                        new KernelActionDirective("#r"),
                        new KernelActionDirective("#!lsmagic"),
                        new KernelActionDirective("#!who"),
                        new KernelActionDirective("#!whos"),
                        new KernelActionDirective("#!set")
                        {
                            Parameters =
                            {
                                new("--name")
                                {
                                    Required = true
                                },
                                new("--value")
                                {
                                    Required = true
                                },
                            },
                            KernelCommandType = typeof(SendValue)
                        }
                    }
                },
                new KernelInfo("pwsh", ["powershell"]),
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

    public bool CreateDbContext { get; set; }
}

internal class SendValue : KernelCommand
{
    public SendValue(
        string name,
        object value,
        FormattedValue formattedValue = null,
        string targetKernelName = null) : base(targetKernelName)
    {
        if (formattedValue is null)
        {
            formattedValue = FormattedValue.CreateSingleFromObject(value, JsonFormatter.MimeType);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        FormattedValue = formattedValue;
    }

    public FormattedValue FormattedValue { get; }

    public string Name { get; }
}

internal class TestCommand : KernelCommand
{
    public string StringProperty { get; set; }

    public int IntProperty { get; set; }
}

public class FormattedValue
{
    public FormattedValue(string mimeType, string value)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
        }

        MimeType = mimeType;
        Value = value;
    }

    public string MimeType { get; }

    public string Value { get; }

    public bool SuppressDisplay { get; set; }

    public static FormattedValue CreateSingleFromObject(object value, string mimeType = null)
    {
        if (mimeType is null)
        {
            mimeType = Formatter.GetPreferredMimeTypesFor(value?.GetType()).First();
        }

        return new FormattedValue(mimeType, value.ToDisplayString(mimeType));
    }

    public static IReadOnlyList<FormattedValue> CreateManyFromObject(object value, params string[] mimeTypes)
    {
        if (mimeTypes is null || mimeTypes.Length == 0)
        {
            mimeTypes = Formatter.GetPreferredMimeTypesFor(value?.GetType()).ToArray();
        }

        var formattedValues =
            mimeTypes
                .Select(mimeType => new FormattedValue(mimeType, value.ToDisplayString(mimeType)))
                .ToArray();

        return formattedValues;
    }
}
