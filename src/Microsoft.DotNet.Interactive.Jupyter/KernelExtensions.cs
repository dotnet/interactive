// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Markdig.Renderers;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Jupyter;

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

        var directive = new KernelActionDirective("#!markdown")
        {
            Description= LocalizationResources.Magics_markdown_Description()
        };

        kernel.AddDirective(directive, RenderMarkdownAsHtml);

        Task RenderMarkdownAsHtml(KernelCommand _, KernelInvocationContext context)
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

            return Task.CompletedTask;
        }

        return kernel;
    }

    private static T UseTime<T>(this T kernel)
        where T : Kernel
    {
        kernel.AddDirective(
            new KernelActionDirective("#!time")
            {
                Description = LocalizationResources.Magics_time_Description()
            },
            MeasureTime);

        return kernel;

        static Task MeasureTime(KernelCommand command, KernelInvocationContext context)
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
            });

            return Task.CompletedTask;
        }
    }

    private static T UseLsMagic<T>(this T kernel)
        where T : Kernel
    {
        kernel.VisitSubkernelsAndSelf(k => k.AddDirective(lsmagic(), Handle));

        Formatter.Register<SupportedDirectives>((directives, context) =>
        {
            var indentLevel = 1.5;
            PocketView t = div(
                h3(directives.KernelName + " kernel"),
                div(directives.Directives.Select(v => div[style: $"text-indent:{indentLevel:##.#}em"](Summary(v, 0)))));

            t.WriteTo(context);

            IEnumerable<IHtmlContent> Summary(KernelDirective directive, double offset)
            {
                yield return new HtmlString("<pre>");

                var level = indentLevel + offset;

                yield return span[style: $"text-indent:{level:##.#}em; color:#512bd4"](directive.Name);

                var nextLevel = (indentLevel * 2) + offset;
                yield return new HtmlString("</pre>");

                yield return div[style: $"text-indent:{nextLevel:##.#}em"](directive.Description);

                if (directive is KernelActionDirective actionDirective)
                {
                    foreach (var subCommand in actionDirective.Subcommands)
                    {
                        yield return div[style: $"text-indent:{nextLevel:##.#}em"](Summary(subCommand, nextLevel));
                    }
                }
            }

            return true;
        }, "text/html");

        return kernel;

        static Task Handle(KernelCommand _, KernelInvocationContext context)
        {
            var rootKernel = context.HandlingKernel.RootKernel;

            DisplayDirectives(rootKernel, context);

            if (rootKernel is CompositeKernel compositeKernel)
            {
                foreach (var subkernel in compositeKernel)
                {
                    if (subkernel.KernelInfo.SupportedDirectives.Any(d => d.Name is "#!lsmagic"))
                    {
                        DisplayDirectives(subkernel, context);
                    }
                }
            }

            return Task.CompletedTask;

            static void DisplayDirectives(Kernel kernel, KernelInvocationContext context)
            {
                var supportedDirectives = new SupportedDirectives(
                    kernel.Name, 
                    kernel.KernelInfo
                              .SupportedDirectives
                              .Where(d => !d.Hidden)
                              .OrderBy(d => d.Name)
                              .ToArray());

                context.Display(supportedDirectives);
            }
        }
    }

    private static KernelActionDirective lsmagic()
    {
        return new KernelActionDirective("#!lsmagic")
        {
            Description = LocalizationResources.Magics_lsmagic_Description()
        };
    }
}