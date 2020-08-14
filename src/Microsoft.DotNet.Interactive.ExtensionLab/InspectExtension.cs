using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ExtensionLab.Inspector;
using Microsoft.DotNet.Interactive.Formatting;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class InspectExtension : IKernelExtension
    {
        private const string INSPECT_COMMAND = "#!inspect";
        public Task OnLoadAsync(Kernel kernel)
        {
            var inspect = new Command(INSPECT_COMMAND, "Inspect the following code in the submission")
            {
                Handler = CommandHandler.Create((KernelInvocationContext context) => Inspect(context))
            };

            kernel.AddDirective(inspect);


            return Task.CompletedTask;
        }

        private async Task Inspect(KernelInvocationContext context)
        {
            if (!(context.Command is SubmitCode))
                return;

            var command = context.Command as SubmitCode;

            // TODO: Is there a "more" proper way of cleaning up code from the magic commands?
            // TODO: CLean the whole line once settings are supported.
            var code = command.Code.Replace(INSPECT_COMMAND, "").Trim();

            var options = new InspectionOptions
            {
                CompilationLanguage = InspectionOptions.LanguageVersion.CSHARP_PREVIEW,
                DecompilationLanguage = InspectionOptions.LanguageVersion.CSHARP_PREVIEW,
                Kind = CodeAnalysis.SourceCodeKind.Script,
                OptimizationLevel = CodeAnalysis.OptimizationLevel.Debug,
                Platform = CodeAnalysis.Platform.AnyCpu
            };

            var inspect = Inspector.Inspector.Create(options);

            var result = inspect.Compile(code);

            if (!result.IsSuccess)
            {
                context.Publish(
                    new ErrorProduced("Uh-oh, something went wrong!", context.Command,
                        new[] {
                            new FormattedValue(PlainTextFormatter.MimeType, string.Join('\n', result.CompilationDiagnostics))
                        }));
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
            PocketView styles = style[type: "text/css"](TabbedCss());

            // TODO: Quite ugly temporary formatting.
            static IEnumerable<string> SplitWithNewline(string text) => text.Split(Environment.NewLine);

            return div[@class: "tab-wrap"](styles,
                input[type: "radio", name: "tabs", id: "tab1", @checked: "checked"],
                div[@class:"tab-label-content", id:"tab1-content"](
                  label[@for:"tab1"]("C#"),
                  div[@class:"tab-content"](pre[@class: "code"](SplitWithNewline(cs).Select(l => span[@class: "line"](l))))),

                input[type:"radio", name:"tabs", id:"tab2"],
                div[@class:"tab-label-content", id:"tab2-content"](
                  label[@for:"tab2"]("IL"),
                  div[@class:"tab-content"](pre[@class: "code"](SplitWithNewline(il).Select(l => span[@class: "line"](l))))),

                input[type:"radio", name:"tabs", id:"tab3"],
                div[@class:"tab-label-content", id:"tab3-content"](
                  label[@for:"tab3"]("JIT ASm"),
                  div[@class:"tab-content"](pre[@class:"code"](SplitWithNewline(jit).Select(l => span[@class: "line"](l)))))
            );
        }

        private static IHtmlContent TabbedCss()
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
                    color: red;
                    border-bottom: 3px solid red;
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
                    font-family: monospace;
                    padding: 0.5em;
                    line-height: 0;
                    counter-reset: line;
                }
                .code .line {
                    display: block;
                    line-height: 1.2rem;
                }
                .code .line:before {
                    counter-increment: line;
                    content: counter(line);
                    display: inline-block;
                    border-right: 1px solid #ddd;
                    padding: 0;
                    margin-right: 1em;
                    color: #888;
                    width: 5ch;
                }
");
        }
    }
}
