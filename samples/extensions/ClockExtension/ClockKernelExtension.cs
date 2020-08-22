// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

namespace ClockExtension
{
    public class ClockKernelExtension : IKernelExtension
    {
        public async Task OnLoadAsync(Kernel kernel)
        {
            Formatter.Register<DateTime>((date, writer) =>
            {
                writer.Write(date.DrawSvgClock());
            }, "text/html");

            Formatter.Register<DateTimeOffset>((date, writer) =>
            {
                writer.Write(date.DrawSvgClock());
            }, "text/html");


            var clockCommand = new Command("#!clock", "Displays a clock showing the current or specified time.")
                {
                    new Option<int>(new[]{"-o","--hour"},
                                    "The position of the hour hand"),
                    new Option<int>(new[]{"-m","--minute"},
                                    "The position of the minute hand"),
                    new Option<int>(new[]{"-s","--second"},
                                    "The position of the second hand")
                };

            clockCommand.Handler = CommandHandler.Create(
                async (int hour, int minute, int second, KernelInvocationContext context) =>
            {
                await context.DisplayAsync(SvgClock.DrawSvgClock(hour, minute, second));
            });

            kernel.AddDirective(clockCommand);


            if (KernelInvocationContext.Current is { } context)
            {
                await context.DisplayAsync($"`{nameof(ClockExtension)}` is loaded. It adds visualizations for `System.DateTime` and `System.DateTimeOffset`. Try it by running: `display(DateTime.Now);` or `#!clock -h`", "text/markdown");
            }
        }
    }
}