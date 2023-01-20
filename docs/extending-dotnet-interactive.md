# Extending .NET Interactive

You can create extensions for .NET Interactive in order to create custom experiences including custom visualizations, new magic commands, new subkernels supporting additional languages, and more. Extensions can be distributed using NuGet packages. The extension is activated when the package is loaded by .NET Interactive using [`#r nuget`](nuget-overview.md).

The simplest way to create an extension is to add a file called `extension.dib` into a NuGet package and build the package so that `extension.dib` is placed in a folder inside the package called `interactive-extensions\dotnet`. 

When a package is loaded using `#r nuget`, if that file is found inside the package, then it will be run the code in `extension.dib` after referencing your packages library (assuming the package contains a library). Because `.dib` files support multiple languages, your extension can run code using any of the supported .NET Interactive languages, or even load new kernels with additional languages. The most common approach, though, and the one detailed below, is to use `extension.dib` to simply call a method in your library.

## Sample project

The [ClockExtension](../samples/extensions/ClockExtension) sample illustrates this approach. There's also walkthrough [in the form of a notebook](../samples/extensions/ClockExtension.ipynb) that shows you how to build and install it.

Here are most important details of the sample project:

* The project contains a public [`Load` method](../samples/extensions/ClockExtension/ClockKernelExtension.cs) containing code to extend .NET Interactive. This can be any public method. Taking a `Kernel` parameter makes it straightforward to write [tests](../samples/extensions/SampleExtensions.Tests/ClockExtensionTests.cs) for your extension code.

* The [`extension.dib`](../samples/extensions/ClockExtension/extension.dib) file contains code to call this method, passing in the root kernel. (Note that this code doesn't contain `using` directives so as not to clutter up the notebook user's IntelliSense.)
    ```csharp
    #!csharp

    ClockExtension.ClockKernelExtension.Load(Microsoft.DotNet.Interactive.KernelInvocationContext.Current.HandlingKernel.RootKernel);
    ```

* [`ClockExtension.csproj`](../samples/extensions/ClockExtension/ClockExtension.csproj) includes a reference to the `extension.dib` file that places it in the correct location in the NuGet package:

    ```xml
    <ItemGroup>
        <None Include="extension.dib" Pack="true" PackagePath="interactive-extensions/dotnet" />
    </ItemGroup>
    ```

Give the [sample notebook](../samples/extensions/ClockExtension.ipynb) a try to see it working end to end on your machine.

## What can you do with an extension?

Here are some of the more common things you might want to do when extending .NET Interactive.

## Add magic commands

You can add to the set of magic commands available in your notebooks. A magic command is defined using [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) to parse the user's input as well as provide help and completions. A `System.CommandLine.Command` is used to define a magic command. The handler you define for the command will be invoked when the user runs the magic command.

Here's an example from the `ClockExtension` sample, showing how to define the `#!clock` magic command:

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

System.CommandLine allows users to get help for a magic command just like they can get help on in a command line app. For example, to get help for the `#!clock` magic command, you can run `#!clock -h`. That produces the following output:

```console
Description:
  Displays a clock showing the current or specified time.

Usage:
  #!clock [options]

Options:
  -o, --hour <hour>      The position of the hour hand
  -m, --minute <minute>  The position of the minute hand
  -s, --second <second>  The position of the second hand
  -?, -h, --help         Show help and usage information
```

By calling the `#!clock` magic command, you can draw a lovely purple clock using SVG with the hands at the positions specified:

<img width="513" alt="image" src="https://user-images.githubusercontent.com/547415/213597279-a5bf64b4-f6de-4f78-a29c-af3c6052677c.png">


The extension also changes the default formatting for the `System.DateTime` type. This feature is the basis for creating custom visualizations for any .NET type. Before installing the extension, the default output just used the `DateTime.ToString` method:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/213597570-aecf2aa1-65bb-46ea-bb09-bd998e0b8fff.png">

After installing the extension, we get the much more appealing clock drawing, with the hands set to the current time:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/213597609-cf5f946b-a6d4-4780-940c-c7cdda1c6017.png">

The code that does this is also found in the sample's `Load` method:

```csharp
Formatter.Register<DateTime>((date, writer) =>
{
    writer.Write(date.DrawSvgClock());
}, "text/html");
```

The `Formatter` API can be used to customize the output for a given .NET type (`System.DateTime`) for a mime type (`"txt/html"`).
