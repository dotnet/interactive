// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Markdig;
using Markdig.Renderers;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class KernelExtensions
    {
        public static T UseDefaultMagicCommands<T>(this T kernel)
            where T : Kernel
        {
            kernel.UseLsMagic()
                  .UseMarkdown()
                  .UseTime();

            return kernel;
        }

        public static CSharpKernel UseJupyterHelpers(
            this CSharpKernel kernel)
        {
            var command = new SubmitCode($@"
#r ""{typeof(TopLevelMethods).Assembly.Location.Replace("\\", "/")}""
using static {typeof(TopLevelMethods).FullName};
");

            kernel.DeferCommand(command);
            return kernel;
        }

        public static PowerShellKernel UseJupyterHelpers(
            this PowerShellKernel kernel)
        {
            kernel.ReadInput = TopLevelMethods.input;
            kernel.ReadPassword = TopLevelMethods.password;
            return kernel;
        }

        private static T UseMarkdown<T>(this T kernel)
            where T : Kernel
        {
            var pipeline = new MarkdownPipelineBuilder()
                   .UseMathematics()
                   .UseAdvancedExtensions()
                   .Build();

            kernel.AddDirective(new Command("#!markdown", "Convert the code that follows from Markdown into HTML")
            {
                Handler = CommandHandler.Create((KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var markdown = submitCode.Code
                                                 .Replace("#!markdown", "")
                                                 .Trim();

                        var document = Markdown.Parse(
                            markdown,
                            pipeline);

                        string html;

                        using (var writer = new StringWriter())
                        {
                            var renderer = new HtmlRenderer(writer);
                            pipeline.Setup(renderer);
                            renderer.Render(document);
                            html = writer.ToString();
                        }

                        context.Publish(
                            new DisplayedValueProduced(
                                html,
                                context.Command,
                                new[]
                                {
                                    new FormattedValue("text/html", html)
                                }));

                        context.Complete(submitCode);
                    }
                })
            });

            return kernel;
        }

        private static T UseTime<T>(this T kernel)
            where T : Kernel
        {
            kernel.AddDirective(time());

            return kernel;

            static Command time()
            {
                return new Command("#!time", "Time the execution of the following code in the submission.")
                {
                    Handler = CommandHandler.Create((KernelInvocationContext context) =>
                    {
                        var timer = new Stopwatch();
                        timer.Start();

                        context.OnComplete(invocationContext =>
                        {
                            var elapsed = timer.Elapsed;

                            invocationContext.Publish(
                                new DisplayedValueProduced(
                                    elapsed,
                                    context.Command,
                                    new[]
                                    {
                                        new FormattedValue(
                                            PlainTextFormatter.MimeType,
                                            $"Wall time: {elapsed.TotalMilliseconds}ms")
                                    }));

                            return Task.CompletedTask;
                        });

                        return Task.CompletedTask;
                    })
                };
            }
        }

        private static T UseLsMagic<T>(this T kernel)
            where T : Kernel
        {
            kernel.AddDirective(lsmagic(kernel));

            kernel.VisitSubkernels(k =>
            {
                k.AddDirective(lsmagic(k));
            });

            Formatter.Register<SupportedDirectives>((directives, writer) =>
            {
                var indentLevel = 1.5;
                PocketView t = div(
                    h3(directives.KernelName + " kernel"),
                    div(directives.Commands.Select(v => div[style: $"text-indent:{indentLevel:##.#}em"](Summary(v, 0)))));

                t.WriteTo(writer, HtmlEncoder.Default);

                IEnumerable<IHtmlContent> Summary(ICommand command, double offset)
                {
                    yield return new HtmlString("<pre>");

                    var level = indentLevel + offset;

                    for (var i = 0; i < command.Aliases.ToArray().Length; i++)
                    {
                        var alias = command.Aliases.ToArray()[i];
                        yield return span[style: $"text-indent:{level:##.#}em; color:#512bd4"](alias);

                        if (i < command.Aliases.Count - 1)
                        {
                            yield return span[style: $"text-indent:{level:##.#}em; color:darkgray"](", ");
                        }
                    }

                    var nextLevel = (indentLevel * 2) + offset;
                    yield return new HtmlString("</pre>");

                    yield return div[style: $"text-indent:{nextLevel:##.#}em"](command.Description);

                    foreach (var subCommand in command.Children.OfType<ICommand>())
                    {
                        yield return div[style: $"text-indent:{nextLevel:##.#}em"](Summary(subCommand, nextLevel));
                    }

                }
            }, "text/html");

            return kernel;
        }

        private static Command lsmagic(Kernel kernel)
        {
            return new Command("#!lsmagic", "List the available magic commands / directives")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
                {
                    var supportedDirectives = new SupportedDirectives(kernel.Name);

                    supportedDirectives.Commands.AddRange(
                        kernel.Directives.Where(d => !d.IsHidden));

                    await context.DisplayAsync(supportedDirectives);

                    await kernel.VisitSubkernelsAsync(async k =>
                    {
                        if (k.Directives.Any(d => d.Name == "#!lsmagic"))
                        {
                            await k.SendAsync(new SubmitCode(((SubmitCode) context.Command).Code));
                        }
                    });
                })
            };
        }
    }
}