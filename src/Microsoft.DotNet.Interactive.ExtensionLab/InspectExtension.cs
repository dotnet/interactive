// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Html;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ExtensionLab.Inspector;
using Microsoft.DotNet.Interactive.Formatting;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.ExtensionLab;

public class InspectExtension
{
    private const string InspectCommand = "inspect";
    public static Task LoadAsync(Kernel kernel)
    {
        var inspect = new Command($"#!{InspectCommand}", "Inspect the following code in the submission")
        {
            new Option<OptimizationLevel>(
                new [] {"-c", "--configuration"},
                getDefaultValue: () => OptimizationLevel.Debug,
                description: "Build configuration to use. Debug or Release."),
            new Option<SourceCodeKind>(
                new [] {"-k", "--kind"},
                getDefaultValue: () => SourceCodeKind.Script,
                description: "Source code kind. Script or Regular."),
        };

        inspect.Handler = CommandHandler.Create((OptimizationLevel configuration, SourceCodeKind kind, Platform platform, KernelInvocationContext context) => Inspect(configuration, kind, platform, context));

        kernel.AddDirective(inspect);

        KernelInvocationContext.Current?.Display(
            new HtmlString(@"<details><summary>Inspect code compilation details using the <code>#!inspect</code> magic command.</summary>
    <p>The <code>#!inspect</code> magic command allows you to see the C# decompilation, IL, and JIT Asm for the code in a C# cell.</p>
    <img src=""https://user-images.githubusercontent.com/547415/109560515-d5749a00-7a90-11eb-9fa3-51b737345bb4.png"" width=""75%"" />
    </details>"),
            "text/html");

        return Task.CompletedTask;
    }

    private static void Inspect(OptimizationLevel configuration, SourceCodeKind kind, Platform platform, KernelInvocationContext context)
    {
        if (context.Command is not SubmitCode command)
        {
            return;
        }

        // TODO: Is there a proper way of cleaning up code from the magic commands?
        var code = Regex.Replace(command.Code, $"#!{InspectCommand}(.*)", "");

        var options = new InspectionOptions
        {
            CompilationLanguage = InspectionOptions.LanguageVersion.CSHARP_PREVIEW,
            DecompilationLanguage = InspectionOptions.LanguageVersion.CSHARP_PREVIEW,
            Kind = kind,
            OptimizationLevel = configuration,
            Platform = Platform.AnyCpu
        };

        var inspect = Inspector.Inspector.Create(options);

        var result = inspect.Compile(code);

        if (!result.IsSuccess)
        {
            var diagnostics = string.Join('\n', result.CompilationDiagnostics);
            context.Fail(command, message: $"Uh-oh, something went wrong:\n {diagnostics}");
            return;
        }

        var htmlResults = RenderResultInTabs(result.CSharpDecompilation, result.ILDecompilation, result.JitDecompilation);

        context.Publish(
            new DisplayedValueProduced(
                htmlResults,
                context.Command,
                new[]
                {
                    new FormattedValue("text/html", htmlResults.ToString())
                }));
    }

    private static PocketView RenderResultInTabs(string cs, string il, string jit)
    {
        PocketView styles = style[type: "text/css"](GetCss());

        var prismScripts = GetPrismJS();
        var prismStyles = GetPrismCSS();

        return div[@class: "tab-wrap"](prismStyles, styles,
            input[type: "radio", name: "tabs", id: "tab1", @checked: "checked"],
            div[@class:"tab-label-content", id:"tab1-content"](
                label[@for:"tab1"]("C#"),
                div[@class:"tab-content"](pre[@class: "code line-numbers"](code[@class: "language-csharp"](cs)))),

            input[type:"radio", name:"tabs", id:"tab2"],
            div[@class:"tab-label-content", id:"tab2-content"](
                label[@for:"tab2"]("IL"),
                div[@class:"tab-content"](pre[@class: "code line-numbers"](code[@class: "language-cil"](il)))),

            input[type:"radio", name:"tabs", id:"tab3"],
            div[@class:"tab-label-content", id:"tab3-content"](
                label[@for:"tab3"]("JIT Asm"),
                div[@class:"tab-content"](pre[@class: "code line-numbers"](code[@class: "language-nasm"](jit)))),
            prismScripts
        );
    }

    private static IHtmlContent GetPrismJS()
    {
        return new HtmlString(@"
<script src=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/prism.min.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/plugins/match-braces/prism-match-braces.min.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/plugins/line-numbers/prism-line-numbers.min.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/plugins/line-highlight/prism-line-highlight.min.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/plugins/autoloader/prism-autoloader.min.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/components/prism-csharp.min.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/components/prism-cil.min.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/components/prism-nasm.min.js""></script>
<script>
    if (document.readyState === ""complete"" || (document.readyState !== ""loading"" && !document.documentElement.doScroll))
    {
        Prism.highlightAll();
    }
    document.addEventListener(""DOMContentLoaded"", Prism.highlightAll);
</script>");
    }

    private static IHtmlContent GetPrismCSS()
    {
        return new HtmlString(@"
<link href=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/themes/prism-coy.min.css"" rel=""stylesheet""/>
<link href=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/plugins/match-braces/prism-match-braces.min.css"" rel=""stylesheet""/>
<link href=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/plugins/line-numbers/prism-line-numbers.min.css"" rel=""stylesheet""/>
<link href=""https://cdn.jsdelivr.net/npm/prismjs@1.21.0/plugins/line-highlight/prism-line-highlight.min.css"" rel=""stylesheet""/>");
    }

    private static IHtmlContent GetCss()
    {
        return new HtmlString(
            @"
                /* Tabbed view */
                .tab-wrap {
                    width: 100%;
                    margin: 0 auto;
                    position: relative;
                    display: -webkit-box;
                    display: flex;
                    top: 0;
                    min-height: 800px;
                }
                .tab-label-content {
                    min-height: 800px;
                    width: 100%;
                }
                .tab-label-content .tab-content {
                    width: 99%;
                    position: absolute;
                    top: 20px;
                    left: 16px;
                    display: none;
                }
                input[type=""radio""][name=""tabs""] {
                    position: relative;
                    z-index: 10;
                    border-bottom: 3px solid transparent;
                    display: none;
                }
                input[type=""radio""][name=""tabs""]:checked + .tab-label-content label {
                    color: #333;
                    font-weight: 800;
                    border-bottom: 3px solid #777;
                }
                input[type=""radio""][name=""tabs""]:checked + .tab-label-content .tab-content {
                    display: block;
                }

                label {
                    cursor: pointer;
                    color: #1b1b1b;
                    box-sizing: border-box;
                    display: -webkit-inline-box;
                    display: inline-flex;
                    -webkit-box-align: center;
                    align-items: center;
                    -webkit-box-pack: center;
                    justify-content: center;
                    text-align: center;
                    height: 30px;
                    -webkit-transition: color 0.2s ease;
                    transition: color 0.2s ease;
                    width: 100%;
                }
                .code {
                    width: 100%;
                    top: 1.5em;
                    left: -1em;
                }
                pre[class*=language-]:after, pre[class*=language-]:before {
                    box-shadow: none !important;
                }
                code[class*=language-], pre[class*=language-] {
                    font-size: 1em !important;
                }

");
    }
}