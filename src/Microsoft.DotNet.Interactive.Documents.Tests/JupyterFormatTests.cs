// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Assent;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Json;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Documents.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public class JupyterFormatTests : DocumentFormatTestsBase
{
    private readonly Configuration _assentConfiguration =
        new Configuration()
            .UsingExtension("json")
            .SetInteractive(Debugger.IsAttached);

    public InteractiveDocument SerializeAndParse(object jupyter)
    {
        var content = JsonConvert.SerializeObject(jupyter);

        return Notebook.Parse(content, DefaultKernelInfos);
    }

    [Theory]
    [InlineData("csharp", "C#", ".cs", "8.0")]
    [InlineData("fsharp", "F#", ".fs", "5.0")]
    public void notebook_metadata_default_language_is_honored_in_cells_without_language_specifier_set(string language, string shortLanguage, string extension, string version)
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 0,
                    source = new[] { "// this is the code" }
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = $".NET ({shortLanguage})",
                    language = shortLanguage,
                    name = $".net-{language}"
                },
                language_info = new
                {
                    file_extension = extension,
                    mimetype = $"text/x-{language}",
                    name = shortLanguage,
                    pygments_lexer = language,
                    version
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("// this is the code", language)
                });
    }

    [Theory]
    [InlineData("C#", "csharp")]
    [InlineData("F#", "fsharp")]
    [InlineData("f#", "fsharp")]
    [InlineData("PowerShell", "powershell")]
    public void Metadata_default_kernel_name_is_based_on_specified_language(string languageName, string kernelName)
    {
        var document = Notebook.Parse(new InteractiveDocument().ToJupyterJson(languageName));

        document.GetDefaultKernelName()
                .Should()
                .Be(kernelName);
    }

    [Theory]
    [InlineData("C#", "csharp")]
    [InlineData("F#", "fsharp")]
    [InlineData("f#", "fsharp")]
    [InlineData("PowerShell", "powershell")]
    public void Metadata_default_kernel_name_is_based_on_specified_language_when_serialized_and_deserialized(string languageName, string kernelName)
    {
        var originalDoc = Notebook.Parse(new InteractiveDocument().ToJupyterJson(languageName));

        var parsedDoc = Notebook.Parse(originalDoc.ToJupyterJson());

        parsedDoc.GetDefaultKernelName()
                 .Should()
                 .Be(kernelName);
    }

    [Fact]
    public void missing_metadata_defaults_to_csharp_kernel()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 0,
                    source = new[] { "// this is assumed to be csharp" }
                },
                new
                {
                    cell_type = "code",
                    execution_count = 0,
                    source = new[]
                    {
                        "#!csharp\n",
                        "// this is still assumed to be csharp"
                    }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("// this is assumed to be csharp", "csharp"),
                    new InteractiveDocumentElement("#!csharp\n// this is still assumed to be csharp", "csharp")
                });
    }

    [Fact]
    public void cell_dotnet_metadata_can_specify_language_that_overrides_notebook()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new
                    {
                        dotnet_interactive = new
                        {
                            language = "fsharp"
                        }
                    },
                    source = new[] { "// this should be F#" }
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = ".NET (C#)",
                    language = "C#",
                    name = ".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = "text/x-csharp",
                    name = "C#",
                    pygments_lexer = "C#",
                    version = "8.0"
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .KernelName
                .Should()
                .Be("fsharp");
    }

    [Fact]
    public void cell_polyglot_metadata_can_specify_kernel_name_that_overrides_notebook()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new
                    {
                        polyglot_notebook = new
                        {
                            kernelName = "fsharp"
                        }
                    },
                    source = new[] { "// this should be F#" }
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = ".NET (C#)",
                    language = "C#",
                    name = ".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = "text/x-csharp",
                    name = "C#",
                    pygments_lexer = "C#",
                    version = "8.0"
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .KernelName
                .Should()
                .Be("fsharp");
    }

    [Fact]
    public void cell_language_can_specify_language_when_there_is_no_notebook_default()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new
                    {
                        dotnet_interactive = new
                        {
                            language = "fsharp"
                        }
                    },
                    source = new[] { "// this should be F#" }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .KernelName
                .Should()
                .Be("fsharp");
    }

    [Fact]
    public void cell_polyglot_kernel_name_overrides_dotnet_metadata_language()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new
                    {
                        dotnet_interactive = new
                        {
                            language = "not-fsharp"
                        },
                        polyglot_notebook = new
                        {
                            kernelName = "fsharp"
                        }
                    },
                    source = new[] { "// this should be F#" }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .KernelName
                .Should()
                .Be("fsharp");
    }

    [Fact]
    public void parsed_cells_do_not_contain_redundant_language_specifier()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 0,
                    source = new[] {"#!csharp\n", "// this is the code"}
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = ".NET (C#)",
                    language = "C#",
                    name = ".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = "text/x-csharp",
                    name = "C#",
                    pygments_lexer = "csharp",
                    version = "8.0"
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new object[]
                {
                    new InteractiveDocumentElement("#!csharp\n// this is the code", "csharp")
                });
    }

    [Fact]
    public void kernel_chooser_magic_takes_precedence_over_metadata_language()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new
                    {
                        dotnet_interactive = new
                        {
                            language = "fsharp"
                        }
                    },
                    source = new[]
                    {
                        "#!pwsh\n",
                        "# this is PowerShell and not F#"
                    }
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = ".NET (C#)",
                    language = "C#",
                    name = ".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = "text/x-csharp",
                    name = "C#",
                    pygments_lexer = "C#",
                    version = "8.0"
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .KernelName
                .Should()
                .Be("pwsh");
    }

    [Fact]
    public void parsed_cell_language_aliases_are_normalized()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    source = new[] { "#!c#\n", "// this is csharp 1" }
                },
                new
                {
                    cell_type = "code",
                    source = new[] { "#!C#\n", "// this is csharp 2" }
                },
                new
                {
                    cell_type = "code",
                    source = new[] { "#!f#\n", "// this is fsharp 1" }
                },
                new
                {
                    cell_type = "code",
                    source = new[] { "#!F#\n", "// this is fsharp 2" }
                },
                new
                {
                    cell_type = "code",
                    source = new[] { "#!powershell\n", "# this is pwsh" }
                }
            }
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("#!c#\n// this is csharp 1", "csharp"),
                    new InteractiveDocumentElement("#!C#\n// this is csharp 2", "csharp"),
                    new InteractiveDocumentElement("#!f#\n// this is fsharp 1", "fsharp"),
                    new InteractiveDocumentElement("#!F#\n// this is fsharp 2", "fsharp"),
                    new InteractiveDocumentElement("#!powershell\n# this is pwsh", "pwsh")
                });
    }

    [Fact]
    public void parsed_cells_can_override_default_language_with_language_specifier()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 0,
                    source = new[] { "#!fsharp\n", "// this is the code" }
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = ".NET (C#)",
                    language = "C#",
                    name = ".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = "text/x-csharp",
                    name = "C#",
                    pygments_lexer = "csharp",
                    version = "8.0"
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("#!fsharp\n// this is the code", "fsharp")
                });
    }

    [Fact]
    public void parsed_cells_can_contain_polyglot_blobs_with_appropriate_default_language()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 0,
                    source = new[] {"// this is csharp\n", "#!fsharp\n", "// and this is fsharp"}
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = ".NET (C#)",
                    language = "C#",
                    name = ".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = "text/x-csharp",
                    name = "C#",
                    pygments_lexer = "csharp",
                    version = "8.0"
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("// this is csharp\n#!fsharp\n// and this is fsharp", "csharp")
                });
    }

    [Fact]
    public void parsed_cells_create_non_language_specifier_first_lines_as_magic_commands()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 0,
                    source = new[] {"#!probably-a-magic-command\n// but this is csharp"}
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = ".NET (C#)",
                    language = "C#",
                    name = ".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = "text/x-csharp",
                    name = "C#",
                    pygments_lexer = "csharp",
                    version = "8.0"
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("#!probably-a-magic-command\n// but this is csharp", "csharp")
                });
    }

    [Fact]
    public void markdown_cells_can_be_parsed_as_a_single_string()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "markdown",
                    source = new[] {"This is `markdown`."}
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("This is `markdown`.", "markdown")
                });
    }

    [Fact]
    public void markdown_cells_can_be_parsed_as_a_string_array()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "markdown",
                    source = new[]
                    {
                        "This is `markdown`.\n",
                        "So is this."
                    }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("This is `markdown`.\nSo is this.", "markdown")
                });
    }

    [Fact]
    public void cells_can_specify_source_as_a_string_array()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 0,
                    source = new[]
                    {
                        // all different newline styles are normalized to `\n`
                        "line 1\n",
                        "line 2\n",
                        "line 3\n",
                        "line 4",
                    }
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = ".NET (C#)",
                    language = "C#",
                    name = ".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = "text/x-csharp",
                    name = "C#",
                    pygments_lexer = "csharp",
                    version = "8.0"
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("line 1\nline 2\nline 3\nline 4", "csharp")
                });
    }

    [Fact]
    public void cells_can_specify_source_as_a_single_string()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    source = "line 1\nline 2\nline 3\n"
                }
            },
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("line 1\nline 2\nline 3\n", "csharp")
                });
    }

    [Fact]
    public void cell_with_dotnet_metadata_but_not_language_can_be_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    source = new[]
                    {
                        "// this is not really fsharp"
                    },
                    metadata = new
                    {
                        dotnet_interactive = new
                        {
                            not_a_language = "fsharp"
                        }
                    }
                }
            }
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("// this is not really fsharp", "csharp")
                    {
                        Metadata = new Dictionary<string, object>
                        {
                            ["dotnet_interactive"] = new Dictionary<string, object>
                            {
                                ["not_a_language"] = "fsharp"
                            }
                        }
                    }
                });
    }

    [Fact]
    public void cell_with_polyglot_metadata_but_not_kernel_name_can_be_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    source = new[]
                    {
                        "// this is not really fsharp"
                    },
                    metadata = new
                    {
                        polyglot_notebook = new
                        {
                            not_a_kernel = "fsharp"
                        }
                    }
                }
            }
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("// this is not really fsharp", "csharp")
                    {
                        Metadata = new Dictionary<string, object>
                        {
                            ["polyglot_notebook"] = new Dictionary<string, object>
                            {
                                ["not_a_kernel"] = "fsharp"
                            }
                        }
                    }
                });
    }

    [Fact]
    public void code_cell_without_source_can_be_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    not_source = new[]
                    {
                        "// this isn't code"
                    }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement(kernelName: "csharp")
                });
    }

    [Fact]
    public void cell_display_output_with_object_array_can_be_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new { },
                    source = new[] { "" },
                    outputs = new[]
                    {
                        new
                        {
                            data = new Dictionary<string, object>
                            {
                                { "text/html", new object[] { "line 1", new { the_answer = 42 } } }
                            },
                            metadata = new { },
                            output_type = "display_data"
                        }
                    }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeOfType<DisplayElement>()
                .Which
                .Data
                .Should()
                .ContainKey("text/html")
                .WhoseValue
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(
                    new object[]
                    {
                        "line 1",
                        new Dictionary<string, object>
                        {
                            { "the_answer", 42 }
                        }
                    }
                );
    }


    [Fact]
    public void cell_display_output_without_data_member_can_be_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new { },
                    source =new[] {"//"},
                    outputs = new object[]
                    {
                        new
                        {
                            output_type = "display_data",
                            not_data = new Dictionary<string, string[]>
                            {
                                { "text/html", new[]{"<div>this is html</div>"} }
                            },
                            execution_count = 1,
                            metadata = new { }
                        }
                    }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeOfType<DisplayElement>()
                .Which
                .Data
                .Should()
                .BeEmpty();
    }

    [Fact]
    public void execute_result_output_without_data_member_can_be_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new { },
                    source =new[] {"//"},
                    outputs = new object[]
                    {
                        new
                        {
                            output_type = "execute_result",
                            not_data = new Dictionary<string, string[]>
                            {
                                { "text/html", new[]{"<div>this is html</div>"} }
                            },
                            execution_count = 1,
                            metadata = new { }
                        }
                    }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeOfType<ReturnValueElement>()
                .Which
                .Data
                .Should()
                .BeEmpty();
    }

    [Fact]
    public void cell_stream_output_without_text_member_can_be_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new { },
                    source = new[] { "//" },
                    outputs = new object[]
                    {
                        new
                        {
                            output_type = "stream",
                            name = "stdout",
                            not_text = "this is text"
                        }
                    }
                }
            }
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeOfType<TextElement>()
                .Which
                .Text
                .Should()
                .BeEmpty();
    }

    [Fact]
    public void cell_error_output_without_required_fields_can_be_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new { },
                    source =new[] {"//"},
                    outputs = new object[]
                    {
                        new
                        {
                            output_type = "error",
                            not_ename = "e-name",
                            not_evalue = "e-value",
                            not_traceback = new[]
                            {
                                "at func1()",
                                "at func2()"
                            }
                        }
                    }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeOfType<ErrorElement>()
                .Which
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new ErrorElement(null, null));
    }

    [Fact]
    public void markdown_cell_missing_source_field_can_be_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "markdown",
                    not_source = new[] { "this isn't markdown" }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new InteractiveDocumentElement(kernelName: "markdown"));
    }

    [Fact]
    public void serialized_notebook_has_appropriate_metadata()
    {
        var notebook = new InteractiveDocument();
        var serialized = notebook.ToJupyterJson("C#");
        var jupyter = JToken.Parse(serialized);

        using var _ = new AssertionScope();
        jupyter["metadata"]
            .Should()
            .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(new
            {
                kernelspec = new
                {
                    display_name = ".NET (C#)",
                    language = "C#",
                    name = ".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = "text/x-csharp",
                    name = "C#",
                    pygments_lexer = "csharp",
                    version = "10.0"
                },
                dotnet_interactive = new
                {
                    defaultKernelName = "csharp",
                    items = new object[]
                    {
                        new { name = "csharp" }
                    }
                },
                polyglot_notebook = new
                {
                    defaultKernelName = "csharp",
                    items = new object[]
                    {
                        new { name = "csharp" }
                    }
                }
            })));
        jupyter["nbformat"]
            .ToObject<int>()
            .Should()
            .Be(4);
        jupyter["nbformat_minor"]
            .ToObject<int>()
            .Should()
            .BeGreaterOrEqualTo(4);
    }

    [Fact]
    public void serialized_code_cells_have_appropriate_shape()
    {
        var cells = new List<InteractiveDocumentElement>
        {
            new("//", "csharp")
        };
        var notebook = new InteractiveDocument(cells);
        var serialized = notebook.ToJupyterJson();
        var jupyter = JToken.Parse(serialized);
        jupyter["cells"]
            .Should()
            .ContainSingleItem()
            .Which
            .Should()
            .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(
                                             new
                                             {
                                                 cell_type = "code",
                                                 execution_count = (int?)null,
                                                 metadata = new
                                                 {
                                                     dotnet_interactive = new
                                                     {
                                                         language = "csharp"
                                                     },
                                                     polyglot_notebook = new
                                                     {
                                                         kernelName = "csharp"
                                                     }
                                                 },
                                                 outputs = Array.Empty<object>(),
                                                 source = new[]
                                                 {
                                                     "//"
                                                 },
                                             }
                                         )));
    }

    [Fact]
    public void serialized_code_cells_with_default_jupyter_kernel_language_dont_have_language_specifier()
    {
        var cells = new List<InteractiveDocumentElement>
        {
            new("var x = 1;", "csharp")
        };
        var notebook = new InteractiveDocument(cells);
        var serialized = notebook.ToJupyterJson();
        var jupyter = JToken.Parse(serialized);
        jupyter["cells"][0]["source"]
            .Should()
            .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(new object[]
            {
                "var x = 1;"
            })));
    }

    [Fact]
    public void serialized_code_cells_with_non_default_jupyter_kernel_language_have_language_metadata_and_no_language_specifier()
    {
        var cells = new List<InteractiveDocumentElement>
        {
            new("let x = 1", "fsharp") { ExecutionOrder = 123 }
        };
        var notebook = new InteractiveDocument(cells);
        var serialized = notebook.ToJupyterJson();
        var jupyter = JToken.Parse(serialized);
        jupyter["cells"][0]
            .Should()
            .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(new
            {
                cell_type = "code",
                execution_count = 123,
                metadata = new
                {
                    dotnet_interactive = new
                    {
                        language = "fsharp"
                    },
                    polyglot_notebook = new
                    {
                        kernelName = "fsharp"
                    }
                },
                source = new[]
                {
                    "let x = 1"
                },
                outputs = new object[] { }
            })));
    }

    [Fact]
    public void code_cells_with_multi_line_text_are_serialized_as_an_array()
    {
        var cells = new List<InteractiveDocumentElement>
        {
            new("var x = 1;\nvar y = 2;", "csharp")
        };
        var notebook = new InteractiveDocument(cells);
        var serialized = notebook.ToJupyterJson();
        var jupyter = JToken.Parse(serialized);
        jupyter["cells"][0]["source"]
            .Should()
            .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(new object[]
            {
                "var x = 1;\n",
                "var y = 2;"
            })));
    }

    [Fact]
    public void serialized_markdown_cells_have_appropriate_shape()
    {
        var cells = new List<InteractiveDocumentElement>
        {
            new("This is `markdown`.\nThis is more `markdown`.", "markdown")
        };
        var notebook = new InteractiveDocument(cells);
        var serialized = notebook.ToJupyterJson();
        var jupyter = JToken.Parse(serialized);
        jupyter["cells"]
            .Should()
            .ContainSingleItem()
            .Which
            .Should()
            .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(
                                             new
                                             {
                                                 cell_type = "markdown",
                                                 metadata = new { },
                                                 source = new[]
                                                 {
                                                     "This is `markdown`.\n",
                                                     "This is more `markdown`."
                                                 }
                                             }
                                         )));
    }

    [Fact]
    public void text_cell_outputs_are_serialized()
    {
        var cells = new List<InteractiveDocumentElement>
        {
            new("//", "csharp", new[]
            {
                new TextElement("this is text", "stdout")
            })
        };
        var notebook = new InteractiveDocument(cells);
        var serialized = notebook.ToJupyterJson();
        var jupyter = JToken.Parse(serialized);
        jupyter["cells"]
            .Should()
            .ContainSingleItem()
            .Which["outputs"]
            .Should()
            .ContainSingleItem()
            .Which
            .Should()
            .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(
                                             new
                                             {
                                                 output_type = "stream",
                                                 name = "stdout",
                                                 text = new[] { "this is text" }
                                             }
                                         )));
    }

    [Fact]
    public void text_cell_outputs_are_parsed_as_string()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    source = "//",
                    outputs = new object[]
                    {
                        new
                        {
                            output_type = "stream",
                            name = "stdout",
                            text = "this is text"
                        }
                    }
                }
            }
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new TextElement("this is text", "stdout"));
    }

    [Fact]
    public void text_cell_outputs_are_parsed_as_string_array()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new { },
                    source = new[] { "//" },
                    outputs = new object[]
                    {
                        new
                        {
                            output_type = "stream",
                            name = "stdout",
                            text = new[]
                            {
                                "this is text\n",
                                "so is this"
                            }
                        }
                    }
                }
            }
        };

        var notebook = SerializeAndParse(jupyter);

        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new TextElement("this is text\nso is this", "stdout"));
    }

    [Fact]
    public void rich_cell_outputs_are_serialized()
    {
        var cells = new List<InteractiveDocumentElement>
        {
            new("//", "csharp", new[]
            {
                new DisplayElement(new Dictionary<string, object>
                {
                    { "text/html", new[] { "<div>this is html</div>" } }
                })
            })
        };
        var notebook = new InteractiveDocument(cells);
        var serialized = notebook.ToJupyterJson();
        var jupyter = JToken.Parse(serialized);
        jupyter["cells"]
            .Should()
            .ContainSingleItem()
            .Which["outputs"]
            .Should()
            .ContainSingleItem()
            .Which
            .Should()
            .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(
                                             new
                                             {
                                                 output_type = "display_data",
                                                 data = new Dictionary<string, string[]>
                                                 {
                                                     { "text/html", new[] { "<div>this is html</div>" } }
                                                 },
                                                 metadata = new { }
                                             }
                                         )));
    }

    [Fact]
    public void rich_cell_outputs_are_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new { },
                    source = new[] { "//" },
                    outputs = new object[]
                    {
                        new
                        {
                            output_type = "display_data",
                            data = new Dictionary<string, string[]>
                            {
                                { "text/html", new[] { "<div>this is html</div>" } }
                            },
                        }
                    }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new DisplayElement(new Dictionary<string, object>
                {
                    { "text/html", new[]{"<div>this is html</div>"} }
                }));
    }

    [Fact]
    public void error_cell_outputs_are_serialized()
    {
        var cells = new List<InteractiveDocumentElement>
        {
            new("//", "csharp", new[]
            {
                new ErrorElement("e-value", "e-name", new[] { "at func1()", "at func2()" })
            })
        };
        var notebook = new InteractiveDocument(cells);
        var serialized = (string)notebook.ToJupyterJson();
        var jupyter = JToken.Parse(serialized);
        jupyter["cells"]
            .Should()
            .ContainSingleItem()
            .Which["outputs"]
            .Should()
            .ContainSingleItem()
            .Which
            .Should()
            .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(
                                             new
                                             {
                                                 output_type = "error",
                                                 ename = "e-name",
                                                 evalue = "e-value",
                                                 traceback = new[]
                                                 {
                                                     "at func1()",
                                                     "at func2()"
                                                 }
                                             }
                                         )));
    }

    [Fact]
    public void error_cell_outputs_are_parsed()
    {
        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 1,
                    metadata = new { },
                    source = new[] { "//" },
                    outputs = new object[]
                    {
                        new
                        {
                            output_type = "error",
                            ename = "e-name",
                            evalue = "e-value",
                            traceback = new[]
                            {
                                "at func1()",
                                "at func2()"
                            }
                        }
                    }
                }
            }
        };
        var notebook = SerializeAndParse(jupyter);
        notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new ErrorElement("e-value", "e-name", new[]
                {
                    "at func1()",
                    "at func2()"
                }));
    }

    [Theory]
    [InlineData("", new string[] { })]
    [InlineData("one", new[] { "one" })]
    [InlineData("one\n", new[] { "one\n" })]
    [InlineData("one\r\n", new[] { "one\r\n" })]
    [InlineData("one\ntwo", new[] { "one\n", "two" })]
    [InlineData("one\r\ntwo", new[] { "one\r\n", "two" })]
    public void SplitIntoJupyterFileArray_performs_expected_split_for_ipynb_array_values(string input, string[] expected)
    {
        var lines = input.SplitIntoJupyterFileArray();

        lines.Should().BeEquivalentTo(expected, c => c.WithStrictOrdering());
    }

    [Fact]
    [Trait("Category", "Contracts and serialization")]
    public async Task ipynb_from_Jupyter_can_be_round_tripped_through_read_and_write_without_the_content_changing()
    {
        var path = GetNotebookFilePath();

        this.Assent(await RoundTripIpynb(path), _assentConfiguration);
    }

    [Fact]
    [Trait("Category", "Contracts and serialization")]
    public async Task ipynb_from_VSCode_can_be_round_tripped_through_read_and_write_without_the_content_changing()
    {
        var path = GetNotebookFilePath();

        this.Assent(await RoundTripIpynb(path), _assentConfiguration);
    }

    [Fact]
    public void Input_tokens_are_parsed_from_ipynb_files()
    {
        var ipynbJson = new InteractiveDocument
        {
            new("""
                #!value --from-file @input:"Enter a filename" --name myfile
                """)
        }.ToJupyterJson();

        var document = Notebook.Parse(ipynbJson);

        document.GetInputFields(ParseDirectiveLine)
                .Should()
                .ContainSingle()
                .Which
                .ValueName
                .Should()
                .Be("myfile");
    }

    private async Task<string> RoundTripIpynb(string filePath)
    {
        var expectedContent = await File.ReadAllTextAsync(filePath);

        var inputDoc = Notebook.Parse(expectedContent);

        var resultContent = inputDoc.ToJupyterJson();

        if (expectedContent.EndsWith("\r\n"))
        {
            resultContent += "\r\n";
        }
        else if (expectedContent.EndsWith("\n"))
        {
            resultContent += "\n";
        }

        return resultContent;
    }

    private string GetNotebookFilePath([CallerMemberName] string testName = null) =>
        Path.Combine(
            Path.GetDirectoryName(PathToCurrentSourceFile()),
            $"{GetType().Name}.{testName}.approved.json");
}