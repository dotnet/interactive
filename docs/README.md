# .NET Interactive Documentation

_Our documentation is still a work in progress. There are a number of topics listed under [Features](#features) below that don't have outgoing links yet, but we've included them to give a better sense of the current feature set. Please open an issue if you'd like to know more about one of the topics we haven't gotten around to documenting yet._

## FAQ

If you're just starting out here, please refer to our [FAQ](./docs/FAQ.md).

## Using notebooks

* [Support for multiple languages](polyglot.md)
* Display output ([C#](display-output-csharp.md) | F# | PowerShell)
* ["Magic commands"](./magic-commands.md)
* [Import NuGet packages](nuget-overview.md)
* [Running code from other notebooks and source files using `#!import`](import-magic-command.md)
* Language-specific features
    * C#
        * The [C# scripting](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) dialect
        * [PocketView](pocketview.md)
    * F#
        * F# Interactive ([FSI](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/fsharp-interactive/))
    * [HTML and JavaScript](javascript-overview.md)
    * PowerShell
        * [PowerShell profile support](../samples/notebooks/powershell/Docs/Profile%20Support.ipynb)
        * [PowerShell host support](../samples/notebooks/powershell/Docs/Interactive-Host-Experience.ipynb)
        * [AzShell support](../samples/notebooks/powershell/Docs/Interact-With-Azure-Cloud-Shell.ipynb)
* [Getting input from the user](input-prompts.md)
* [Formatter APIs](formatting.md)
    * [PocketView](pocketview.md)
* [Multi-language notebooks](polyglot.md)
    * [.NET variable sharing](variable-sharing.md)
    * [Accessing kernel variables from the client with JavaScript](javascript-overview.md) 

## Technical overview

* [Architecture](kernels-overview.md)
* How Jupyter kernel installation works

## .NET Interactive API Guides

* Using the .NET Interactive [command-line interface](../src/dotnet-interactive/CommandLine/readme.md)

### .NET API Guide ([TODO](https://github.com/dotnet/interactive/issues/815))

* Commands and events
    * Message protocol ([TODO](https://github.com/dotnet/interactive/issues/813))
* Magic commands
* Kernel APIs
    * Variable sharing

### JavaScript API Guide ([TODO](https://github.com/dotnet/interactive/issues/814))

* Loading dependencies
* Sharing data
* Sending kernel commands and consuming kernel events
 
## Extending .NET Interactive

* [Building your own extension](extending-dotnet-interactive.md)
  * [Adding magic commands](extending-dotnet-interactive.md#adding-magic-commands)
* Publishing your extension using NuGet
