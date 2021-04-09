using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using System;
using System.Linq;

var kernel = new FSharpKernel()
                    .UseDefaultFormatting()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseWho();
Formatter.SetPreferredMimeTypeFor(typeof(object), "text/plain");
Formatter.Register<object>(o => o.ToString());

while (true)
{
    if (ReadLine() is not { } request)
        continue;
    var toSubmit = new SubmitCode(request);
    var response = await kernel.SendAsync(toSubmit);
    response.KernelEvents.Subscribe(
        e =>
        {
            switch (e)
            {
                case CommandFailed failed:
                    WriteLineError(failed.Message);
                    break;
                case DisplayEvent display:
                    WriteLine(display.FormattedValues.First().Value);
                    break;
            }
        });
}


static string? ReadLine()
{
    Console.Write("\nInput: ");
    return Console.ReadLine();
}

static void WriteLine(string input)
{
    Console.ForegroundColor = ConsoleColor.DarkGreen;
    Console.WriteLine($"\nOutput: {input}");
    Console.ForegroundColor = ConsoleColor.Gray;
}

static void WriteLineError(string input)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\nError: {input}");
    Console.ForegroundColor = ConsoleColor.Gray;
}