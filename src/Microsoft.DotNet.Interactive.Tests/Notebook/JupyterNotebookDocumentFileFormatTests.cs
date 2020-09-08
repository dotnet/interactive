﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using Assent;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Json;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Notebook;
using Microsoft.PowerShell.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Notebook
{
    public partial class JupyterNotebookDocumentFileFormatTests : NotebookDocumentFileFormatTests
    {
        public JupyterNotebookDocumentFileFormatTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public NotebookDocument ParseJupyter(object jupyter)
        {
            var content = JsonConvert.SerializeObject(jupyter);
            return ParseFromString("notebook.ipynb", content);
        }

        public string SerializeJupyter(NotebookDocument notebook)
        {
            return SerializeToString("notebook.ipynb", notebook);
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
                        execution_count = 1,
                        metadata = new { },
                        source = "// this is the code"
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
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell(language, "// this is the code")
                });
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
                        execution_count = 1,
                        metadata = new { },
                        source = "// this is assumed to be csharp"
                    },
                    new
                    {
                        cell_type = "code",
                        execution_count = 1,
                        metadata = new { },
                        source = "#!csharp\n// this is still assumed to be csharp"
                    }
                }
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("csharp", "// this is assumed to be csharp"),
                    new NotebookCell("csharp", "// this is still assumed to be csharp")
                });
        }

        [Fact]
        public void parsed_cells_dont_contain_redundant_language_specifier()
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
                        source = "#!csharp\n// this is the code"
                    }
                },
                metadata = new
                {
                    kernelspec = new
                    {
                        display_name = $".NET (C#)",
                        language = "C#",
                        name = $".net-csharp"
                    },
                    language_info = new
                    {
                        file_extension = ".cs",
                        mimetype = $"text/x-csharp",
                        name = "C#",
                        pygments_lexer = "csharp",
                        version = "8.0"
                    }
                },
                nbformat = 4,
                nbformat_minor = 4
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("csharp", "// this is the code")
                });
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
                        execution_count = 1,
                        metadata = new { },
                        source = "#!c#\n// this is csharp 1"
                    },
                    new
                    {
                        cell_type = "code",
                        execution_count = 1,
                        metadata = new { },
                        source = "#!C#\n// this is csharp 2"
                    },
                    new
                    {
                        cell_type = "code",
                        execution_count = 1,
                        metadata = new { },
                        source = "#!f#\n// this is fsharp 1"
                    },
                    new
                    {
                        cell_type = "code",
                        execution_count = 1,
                        metadata = new { },
                        source = "#!F#\n// this is fsharp 2"
                    },
                    new
                    {
                        cell_type = "code",
                        execution_count = 1,
                        metadata = new { },
                        source = "#!powershell\n# this is pwsh"
                    }
                }
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("csharp", "// this is csharp 1"),
                    new NotebookCell("csharp", "// this is csharp 2"),
                    new NotebookCell("fsharp", "// this is fsharp 1"),
                    new NotebookCell("fsharp", "// this is fsharp 2"),
                    new NotebookCell("pwsh", "# this is pwsh")
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
                        execution_count = 1,
                        metadata = new { },
                        source = "#!fsharp\n// this is the code"
                    }
                },
                metadata = new
                {
                    kernelspec = new
                    {
                        display_name = $".NET (C#)",
                        language = "C#",
                        name = $".net-csharp"
                    },
                    language_info = new
                    {
                        file_extension = ".cs",
                        mimetype = $"text/x-csharp",
                        name = "C#",
                        pygments_lexer = "csharp",
                        version = "8.0"
                    }
                },
                nbformat = 4,
                nbformat_minor = 4
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("fsharp", "// this is the code")
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
                        execution_count = 1,
                        metadata = new { },
                        source = "// this is csharp\n#!fsharp\n// and this is fsharp"
                    }
                },
                metadata = new
                {
                    kernelspec = new
                    {
                        display_name = $".NET (C#)",
                        language = "C#",
                        name = $".net-csharp"
                    },
                    language_info = new
                    {
                        file_extension = ".cs",
                        mimetype = $"text/x-csharp",
                        name = "C#",
                        pygments_lexer = "csharp",
                        version = "8.0"
                    }
                },
                nbformat = 4,
                nbformat_minor = 4
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("csharp", "// this is csharp\n#!fsharp\n// and this is fsharp")
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
                        execution_count = 1,
                        metadata = new { },
                        source = "#!probably-a-magic-command\n// but this is csharp"
                    }
                },
                metadata = new
                {
                    kernelspec = new
                    {
                        display_name = $".NET (C#)",
                        language = "C#",
                        name = $".net-csharp"
                    },
                    language_info = new
                    {
                        file_extension = ".cs",
                        mimetype = $"text/x-csharp",
                        name = "C#",
                        pygments_lexer = "csharp",
                        version = "8.0"
                    }
                },
                nbformat = 4,
                nbformat_minor = 4
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("csharp", "#!probably-a-magic-command\n// but this is csharp")
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
                        metadata = new { },
                        source = "This is `markdown`."
                    }
                }
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("markdown", "This is `markdown`.")
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
                        metadata = new { },
                        source = new[]
                        {
                            "This is `markdown`.",
                            "So is this."
                        }
                    }
                }
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("markdown", "This is `markdown`.\nSo is this.")
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
                        execution_count = 1,
                        metadata = new { },
                        source = "line 1\nline 2\nline 3\n"
                    }
                },
                metadata = new
                {
                    kernelspec = new
                    {
                        display_name = $".NET (C#)",
                        language = "C#",
                        name = $".net-csharp"
                    },
                    language_info = new
                    {
                        file_extension = ".cs",
                        mimetype = $"text/x-csharp",
                        name = "C#",
                        pygments_lexer = "csharp",
                        version = "8.0"
                    }
                },
                nbformat = 4,
                nbformat_minor = 4
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("csharp", "line 1\nline 2\nline 3\n")
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
                        execution_count = 1,
                        metadata = new { },
                        source = new[]
                        {
                            // all different newline styles are normalized to `\n`
                            "line 1",
                            "line 2\r\n",
                            "line 3\n",
                            "line 4",
                        }
                    }
                },
                metadata = new
                {
                    kernelspec = new
                    {
                        display_name = $".NET (C#)",
                        language = "C#",
                        name = $".net-csharp"
                    },
                    language_info = new
                    {
                        file_extension = ".cs",
                        mimetype = $"text/x-csharp",
                        name = "C#",
                        pygments_lexer = "csharp",
                        version = "8.0"
                    }
                },
                nbformat = 4,
                nbformat_minor = 4
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .BeEquivalentTo(new[]
                {
                    new NotebookCell("csharp", "line 1\nline 2\nline 3\nline 4")
                });
        }

        [Fact]
        public void serialized_notebook_has_appropriate_metadata()
        {
            var notebook = new NotebookDocument(Array.Empty<NotebookCell>());
            var serialized = SerializeJupyter(notebook);
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
                        version = "8.0"
                    }
                })));
            jupyter["nbformat"]
                .ToObject<int>()
                .Should()
                .Be(4);
            jupyter["nbformat_minor"]
                .ToObject<int>()
                .Should()
                .Be(4);
        }

        [Fact]
        public void serialized_code_cells_have_appropriate_shape()
        {
            var cells = new[]
            {
                new NotebookCell("csharp", "//")
            };
            var notebook = new NotebookDocument(cells);
            var serialized = SerializeJupyter(notebook);
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
                        execution_count = 1,
                        metadata = new { },
                        source = new[]
                        {
                            "//"
                        },
                        outputs = Array.Empty<object>()
                    }
                )));
        }

        [Fact]
        public void serialized_code_cells_with_default_jupyter_kernel_language_dont_have_language_specifier()
        {
            var cells = new[]
            {
                new NotebookCell("csharp", "var x = 1;")
            };
            var notebook = new NotebookDocument(cells);
            var serialized = SerializeJupyter(notebook);
            var jupyter = JToken.Parse(serialized);
            jupyter["cells"][0]["source"]
                .Should()
                .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(new object[]
                {
                    "var x = 1;"
                })));
        }

        [Fact]
        public void serialized_code_cells_with_non_default_jupyter_kernel_language_have_language_specifier()
        {
            var cells = new[]
            {
                new NotebookCell("fsharp", "let x = 1")
            };
            var notebook = new NotebookDocument(cells);
            var serialized = SerializeJupyter(notebook);
            var jupyter = JToken.Parse(serialized);
            jupyter["cells"][0]["source"]
                .Should()
                .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(new object[]
                {
                    "#!fsharp\r\n",
                    "let x = 1"
                })));
        }

        [Fact]
        public void code_cells_with_multi_line_text_are_serialized_as_an_array()
        {
            var cells = new[]
            {
                new NotebookCell("csharp", "var x = 1;\nvar y = 2;")
            };
            var notebook = new NotebookDocument(cells);
            var serialized = SerializeJupyter(notebook);
            var jupyter = JToken.Parse(serialized);
            jupyter["cells"][0]["source"]
                .Should()
                .BeEquivalentTo(JToken.Parse(JsonConvert.SerializeObject(new object[]
                {
                    "var x = 1;\r\n",
                    "var y = 2;"
                })));
        }

        [Fact]
        public void serialized_markdown_cells_have_appropriate_shape()
        {
            var cells = new[]
            {
                new NotebookCell("markdown", "This is `markdown`.\nThis is more `markdown`.")
            };
            var notebook = new NotebookDocument(cells);
            var serialized = SerializeJupyter(notebook);
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
                            "This is `markdown`.\r\n",
                            "This is more `markdown`.",
                        }
                    }
                )));
        }

        [Fact]
        public void text_cell_outputs_are_serialized()
        {
            var cells = new[]
            {
                new NotebookCell("csharp", "//", new[]
                {
                    new NotebookCellTextOutput("this is text")
                })
            };
            var notebook = new NotebookDocument(cells);
            var serialized = SerializeJupyter(notebook);
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
                        text = "this is text"
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
                        execution_count = 1,
                        metadata = new { },
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
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentTo(new NotebookCellTextOutput("this is text"));
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
                        source = "//",
                        outputs = new object[]
                        {
                            new
                            {
                                output_type = "stream",
                                name = "stdout",
                                text = new[]
                                {
                                    "this is text",
                                    "so is this"
                                }
                            }
                        }
                    }
                }
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentTo(new NotebookCellTextOutput("this is text\nso is this"));
        }

        [Fact]
        public void rich_cell_outputs_are_serialized()
        {
            var cells = new[]
            {
                new NotebookCell("csharp", "//", new[]
                {
                    new NotebookCellDisplayOutput(new Dictionary<string, object>()
                    {
                        { "text/html", "this is html" }
                    })
                })
            };
            var notebook = new NotebookDocument(cells);
            var serialized = SerializeJupyter(notebook);
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
                        output_type = "execute_result",
                        data = new Dictionary<string, string>()
                        {
                            { "text/html", "this is html" }
                        },
                        execution_count = 1,
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
                        source = "//",
                        outputs = new object[]
                        {
                            new
                            {
                                output_type = "execute_result",
                                data = new Dictionary<string, string>()
                                {
                                    { "text/html", "this is html" }
                                },
                                metadata = new{ }
                            }
                        }
                    }
                }
            };
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentTo(new NotebookCellDisplayOutput(new Dictionary<string, object>()
                {
                    { "text/html", "this is html" }
                }));
        }

        [Fact]
        public void error_cell_outputs_are_serialized()
        {
            var cells = new[]
            {
                new NotebookCell("csharp", "//", new[]
                {
                    new NotebookCellErrorOutput("e-name", "e-value", new[] { "at func1()", "at func2()" })
                })
            };
            var notebook = new NotebookDocument(cells);
            var serialized = SerializeJupyter(notebook);
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
                        source = "//",
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
            var notebook = ParseJupyter(jupyter);
            notebook.Cells
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentTo(new NotebookCellErrorOutput("e-name", "e-value", new[]
                {
                    "at func1()",
                    "at func2()"
                }));
        }

        [Fact]
        public void serialize_entire_file_to_verify_indention()
        {
            var configuration = new Configuration()
                                 .UsingExtension("json")
                                 .SetInteractive(false);
            var cells = new[]
            {
                new NotebookCell("csharp", "// this is csharp", new[]
                {
                    new NotebookCellDisplayOutput(new Dictionary<string, object>()
                    {
                        { "text/html", "this is html" }
                    })
                }),
                new NotebookCell("markdown", "This is `markdown`.")
            };
            var notebook = new NotebookDocument(cells);
            var json = SerializeJupyter(notebook);
            this.Assent(json, configuration);
        }
    }
}
