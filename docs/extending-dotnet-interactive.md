# Extending .NET Interactive

You can create extensions for .NET Interactive in order to create custom experiences including custom visualizations, new magic commands, new subkernels supporting additional languages, and more. Extensions can be distributed using NuGet packages installed using [`#r nuget`](nuget-overview.md).

If you want to look at some example code first, a great place to start is the [ClockExtension](../samples/extensions/ClockExtension) sample. And we have a walkthrough that shows you how to build and install it [in the form of a notebook](../samples/extensions/ClockExtension.ipynb).

Let's walk through the steps involved.

## Implement `IKernelExtension`

The `IKernelExtension` interface is simple:

```csharp
public interface IKernelExtension
{
    Task OnLoadAsync(Kernel kernel);
}
```

When your extension is loaded, this method will be called and a kernel will be passed to it. Typically, in `dotnet-interactive`, this will be a `CompositeKernel` containing the standard language-specific subkernels. Keep in mind though that configurations without a `CompositeKernel` are possible, and your extension should anticipate this. You can read more about about kernels [here](kernels-overview.md).

## Adding magic commands

You can add to the set of magic commands available in your notebooks. Here's an example from the `ClockExtension`:

```csharp
public class ClockKernelExtension : IKernelExtension
{
    public async Task OnLoadAsync(Kernel kernel)
    {
        // ...
        
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
     
        //...
        
        kernel.AddDirective(clockCommand);
        
        // ...
    }
}
```

Once the `Command` has been added using `Kernel.AddDirective`, it's available in the kernel and ready to be used.

The magic command syntax is a command line syntax. It's implemented using the `System.CommandLine` command line [library](https://nuget.org/packages/System.CommandLine). You can get help for a magic command in the same way you can typically get help from a command line tool. Here's the help for the `#!clock` magic command that the previous code produces: 

<img src="https://user-images.githubusercontent.com/547415/82130770-3db4f200-9783-11ea-9912-537127e4ba15.png" width="60%">

By calling the `#!clock` magic command, you can draw a lovely purple clock using SVG with the hands at the positions specified:

<img src="https://user-images.githubusercontent.com/547415/82130789-75239e80-9783-11ea-9bf0-6e17c196148c.png" width="60%">

The extension also changes the default formatting for the `System.DateTime` type. This feature is the basis for creating custom visualizations for any .NET type. Before installing the extension, the default output just used the `DateTime.ToString` method:

<img src="https://user-images.githubusercontent.com/547415/82130856-e2373400-9783-11ea-9582-56bde34f38eb.png" width="60%">

After installing the extension, we get the much more appealing clock drawing, with the hands set to the current time:

<img src="https://user-images.githubusercontent.com/547415/82130861-f11de680-9783-11ea-8159-eeef916db92e.png" width="60%">

The code that does this is also found in the sample's `OnLoadAsync` method:

```csharp
Formatter.Register<DateTime>((date, writer) =>
{
    writer.Write(date.DrawSvgClock());
}, "text/html");
```

The `Formatter` API can be used to customize the output for a given .NET type (`System.DateTime`) for a mime type (`"txt/html"`).

## Script-based extensions

As an alternative to implementing `IKernelExtension`, extensions can also be script-based. This enables a NuGet package to not have any direct dependency on the
`Microsoft.DotNet.Interactive` libraries or tools which means that any other projects that normally reference that
NuGet package also will not have a dependency on .NET Interactive. 

NuGet packages that are loaded via `#r "nuget:..."` are probed for a well-known file, `interactive-extensions/dotnet/extension.dib`.
If that file is found, then it is read in its entirety and executed. The script can add new commands, register
formatters, load packages, and run code in multiple languages using kernel chooser magic commands (e.g. `#!csharp`, `#!fsharp`, `#!pwsh`, `#!javascript`, and so on).  

See the [RandomNumber](../samples/extensions/RandomNumber/README.md) extension to see this in action.
