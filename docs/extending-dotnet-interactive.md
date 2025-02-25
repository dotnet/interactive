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

### Add magic commands

You can create you own magic commands and add them to the available commands in your notebooks either by writing an extension or by defining them directly in the notebook code. Magic commands are defined using the APIs under the `Microsoft.DotNet.Interactive.Directives` namespace to parse the user's input and define the associated actions performed by the magic command, as well as provide hover help and completions.

We'll use the example found in the [`ClockExtension` sample](https://github.com/dotnet/interactive/blob/main/samples/extensions/ClockExtension) to go over the high-level usage of this API. This sample creates a magic command called `#!clock` that can be used to display an SVG rendering of a clock showing the specified time.

<img src="https://github.com/user-attachments/assets/4d5a10c8-daac-444a-9c99-699e526307b9" />

Let's start by looking at the code used to define the magic command itself. This code creates a magic command (`#!clock`) with three parameters (`--hour`, `--minute`, and `--second`), which can be called as in the screen shot above. We'll go through the code step by step to explain how the API is used. 

```csharp
var hourParameter = new KernelDirectiveParameter("--hour", "The position of the hour hand");
var minuteParameter = new KernelDirectiveParameter("--minute", "The position of the minute hand");
var secondParameter = new KernelDirectiveParameter("--second", "The position of the second hand");

var clockDirective = new KernelActionDirective("#!clock")
{
    Description = "Displays a clock showing the current or specified time.",
    Parameters =
    [
        hourParameter,
        minuteParameter,
        secondParameter
    ]
};
```

A `KernelActionDirective` is used to define a magic command and give it a name (`#!clock`), which is the text used in code to invoke it. The `KernelDirectiveParameter` class is used to define the parameters that the magic command accepts.

The names and descriptions for these objects are used to provide hover help and standard completions such as parameter names.

<img src="https://github.com/user-attachments/assets/85adc71a-cf7c-418e-b8b5-a3832576d0a9" />

<img src="https://github.com/user-attachments/assets/78654d82-68c8-49b5-a779-83624565ad4f" />

The next step is to add the `KernelActionDirective` to the kernel where it will be in scope. 

```csharp
kernel.AddDirective<DisplayClock>(clockDirective, (displayClock, context) =>
{
    context.Display(
        SvgClock.DrawSvgClock(
            displayClock.Hour,
            displayClock.Minute, 
            displayClock.Second));
    return Task.CompletedTask;
});
```

Several things are happening in the above code.

* `kernel.AddDirective` adds the the directive (`clockDirective`) to the kernel.
* The generic parameter (`DisplayClock`) defines the type of the associated `KernelCommand` that will be instantiated and sent to the kernel when the magic command is invoked.
* The delegate defines the code that will run to handle the `DisplayClock` command.

Here's the definition of `DisplayClock`:

```csharp
public class DisplayClock : KernelDirectiveCommand
{
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int Second { get; set; }
}
```

Note that `KernelDirectiveCommand` inherits `KernelCommand`, so you can send the `DisplayClock` command directly using the `Kernel.SendAsync` method, just like any other `KernelCommand`.

### Customize formatting

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
