// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace ClockExtension;

public static class ClockKernelExtension
{
    public static void Load(Kernel kernel)
    {
        // Register formatters that will change the default output when formatting DateTime and DateTimeOffset instances as text/html.
        Formatter.Register<DateTime>((date, writer) => writer.Write(date.DrawSvgClock()), "text/html");

        Formatter.Register<DateTimeOffset>((date, writer) => writer.Write(date.DrawSvgClock()), "text/html");

        // Next, define a magic command that will render a clock.
        var hourOption = new Option<int>(new[] { "-o", "--hour" },
                                         "The position of the hour hand");
        var minuteOption = new Option<int>(new[] { "-m", "--minute" },
                                           "The position of the minute hand");
        var secondOption = new Option<int>(new[] { "-s", "--second" },
                                           "The position of the second hand");

        var clockCommand = new Command("#!clock", "Displays a clock showing the current or specified time.")
        {
            hourOption,
            minuteOption,
            secondOption
        };

        clockCommand.SetHandler(
            (hour, minute, second) => KernelInvocationContext.Current.Display(SvgClock.DrawSvgClock(hour, minute, second)),
            hourOption,
            minuteOption,
            secondOption);

        kernel.AddDirective(clockCommand);

        // Finally, display some information to the user so they can see how to use the extension.
        PocketView view = div(
            code(nameof(ClockExtension)),
            " is loaded. It adds visualizations for ",
            code(typeof(DateTime)),
            " and ",
            code(typeof(DateTimeOffset)),
            ". Try it by running: ",
            code("DateTime.Now"),
            " or ",
            code("#!clock -h")
        );

        KernelInvocationContext.Current?.Display(view);
    }
}