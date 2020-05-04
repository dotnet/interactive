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
            where T : KernelBase
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
#r ""{typeof(Kernel).Assembly.Location.Replace("\\", "/")}""
using static {typeof(Kernel).FullName};
");

            kernel.DeferCommand(command);
            return kernel;
        }

        public static PowerShellKernel UseJupyterHelpers(
            this PowerShellKernel kernel)
        {
            kernel.ReadInput = Kernel.input;
            kernel.ReadPassword = Kernel.password;
            return kernel;
        }

        private static T UseMarkdown<T>(this T kernel)
            where T : KernelBase
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
            where T : KernelBase
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
            where T : KernelBase
        {
            kernel.AddDirective(lsmagic());

            kernel.VisitSubkernels(k =>
            {
                if (k is KernelBase kb)
                {
                    kb.AddDirective(lsmagic());
                }
            });

            Formatter<SupportedDirectives>.Register((directives, writer) =>
            {
                PocketView t = div(
                    h3(directives.KernelName + " kernel"),
                    div(directives.Commands.Select(v => div[style: "text-indent:1.5em"](Summary(v)))));

                t.WriteTo(writer, HtmlEncoder.Default);

                IEnumerable<IHtmlContent> Summary(ICommand command)
                {
                    yield return new HtmlString("<pre>");

                    for (var i = 0; i < command.Aliases.Count; i++)
                    {
                        yield return span[style: "color:#512bd4"](command.Aliases[i]);

                        if (i < command.Aliases.Count - 1)
                        {
                            yield return span[style: "color:darkgray"](", ");
                        }
                    }

                    yield return new HtmlString("</pre>");

                    yield return div[style: "text-indent:3em"](command.Description);
                }
            }, "text/html");

            return kernel;
        }

        private static Command lsmagic()
        {
            return new Command("#!lsmagic", "List the available magic commands / directives")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
                {
                    var kernel = context.CurrentKernel;

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