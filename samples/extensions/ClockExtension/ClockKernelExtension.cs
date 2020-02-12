// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

namespace ClockExtension
{
    public class ClockKernelExtension : IKernelExtension
    {
        public async Task OnLoadAsync(IKernel kernel)
        {
            Formatter<DateTime>.Register((d, writer) => { writer.Write(d.DrawSvgClock()); }, "text/html");

            Formatter<DateTimeOffset>.Register((d, writer) => { writer.Write(d.DrawSvgClock()); }, "text/html");

            Formatter.Register(typeof(IObservable<DateTime>), (o, writer) =>
            {
                var ts = o as IObservable<DateTime>;

                var firstAsync = Task.Run(async () => await ts.FirstAsync()).Result;

                writer.Write(firstAsync.DrawSvgClock());
            }, "text/html");

            if (KernelInvocationContext.Current is {} context)
            {
                await context.DisplayAsync($"{nameof(ClockExtension)} is loaded. Now you can format System.DateTime and System.DateTimeOffset.");
            }
        }
    }
}