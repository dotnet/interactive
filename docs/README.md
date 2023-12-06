# Polyglot Notebooks and .NET Interactive Documentation

## FAQ

If you're just starting out here, you might want to start with the [FAQ](./FAQ.md).

## Using Polyglot Notebooks

* [Use "Magic commands"](./magic-commands.md)
* [Share values between languages](variable-sharing.md)
* [Import NuGet packages](nuget-overview.md)
* [Run code from other notebooks and source files using `#!import`](import-magic-command.md)
* [Get input from the user](input-prompts.md)
* [Formatter APIs for .NET](formatting.md)
* Language-specific features
    * C#
        * The [C# Script](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) dialect
        * [PocketView](pocketview.md)
    * F#
        * F# Interactive ([FSI](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/fsharp-interactive/))
    * [JavaScript](javascript-overview.md)
    * PowerShell
        * [PowerShell profile support](../samples/notebooks/powershell/Docs/Profile%20Support.ipynb)
        * [PowerShell host support](../samples/notebooks/powershell/Docs/Interactive-Host-Experience.ipynb)
        * [AzShell support](../samples/notebooks/powershell/Docs/Interact-With-Azure-Cloud-Shell.ipynb)
* Work with HTML
    * [PocketView](pocketview.md)
* [Multi-language notebooks with Jupyter](polyglot-with-jupyter.md)

## Technical overview

* [Architecture](kernels-overview.md)
* [Install as Jupyter kernel](NotebookswithJupyter.md)

## .NET Interactive Developer Documentation

* Using the .NET Interactive [command-line interface](command-line-interface.md)

### .NET API Guide ([TODO](https://github.com/dotnet/interactive/issues/815))

* Commands and events
    * Message protocol ([TODO](https://github.com/dotnet/interactive/issues/813))
* Magic commands
* Kernel APIs
    * Variable sharing

## Extending .NET Interactive

* [Adding support for new Jupyter subkernels](adding-jupyter-kernels.md)
* [Building your own extension](extending-dotnet-interactive.md)
  * [Adding magic commands](extending-dotnet-interactive.md#adding-magic-commands)

