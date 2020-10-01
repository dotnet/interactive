# .NET Interactive Documentation 

_Our documentation is still a work in progress. There are a number of topics listed under [Features](#features) below that don't have outgoing links yet, but we've included them to give a better sense of the current feature set. Please open an issue if you'd like to know more about one of the topics we haven't gotten around to documenting yet._

## Getting started 

### Jupyter and nteract

There are several ways to get started using .NET Interactive with Jupyter, including Jupyter Notebook, JupyterLab, and nteract.

* [Try sample .NET notebooks online using Binder](NotebooksOnBinder.md).
* [Create and run .NET notebooks on your machine](NotebooksLocalExperience.md).
* [Share .NET notebooks online using Binder](CreateBinder.md).

### Visual Studio Code

Work is underway to add support for the new Visual Studio Code [native notebook feature](https://code.visualstudio.com/updates/v1_45#_github-issue-notebook). While we are still in the early stages of this effort, if you'd like to experiment with it you can find instructions [here](../src/dotnet-interactive-vscode/README.md). 

### Small factor devices

We support running on devices like Raspberry Pi and [pi-top [4]](https://github.com/pi-top/pi-top-4-.NET-Core-API). You can find instructions [here](small-factor-devices.md)

### Telemetry

Telemetry is collected when various .NET Interactive command lines are run. Once .NET Interactive is running, we do not collect telemetry from any further user actions. The teletry is anonymous and collected only the values for a specific subset of the verbs on the .NET Interactive CLI. Those verbs are:

* `dotnet interactive jupyter`
* `dotnet interactive jupyter install`
* `dotnet interactive http`
* `dotnet interactive stdio`

#### How to opt out

The .NET Interactive telemetry feature is enabled by default. To opt out of the telemetry feature, set the `DOTNET_TRY_CLI_TELEMETRY_OPTOUT` environment variable to `1` or `true`.

#### Disclosure

The .NET Interactive tool displays text similar to the following when you first run one of the .NET Interactive CLI commands (for example, `dotnet interactive jupyter install`). Text may vary slightly depending on the version of the tool you're running. This "first run" experience is how Microsoft notifies you about data collection.

```console
Telemetry
---------
The .NET Core tools collect usage data in order to help us improve your experience.The data is anonymous and doesn't include command-line arguments. The data is collected by Microsoft and shared with the community. You can opt-out of telemetry by setting the DOTNET_TRY_CLI_TELEMETRY_OPTOUT environment variable to '1' or 'true' using your favorite shell.
```

To disable this message and the .NET Core welcome message, set the `DOTNET_TRY_SKIP_FIRST_TIME_EXPERIENCE` environment variable to `true`. Note that this variable has no effect on telemetry opt out.

## Features

* [Support for multiple languages](polyglot.md)
* Display output ([C#](display-output-csharp.md) | F# | PowerShell)
* Create plots with with [Xplot](https://fslab.org/XPlot/)
* ["Magic commands"](./magic-commands.md)
* [Import NuGet packages](nuget-overview.md)
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
* Getting input from the user
* [Multi-language notebooks](polyglot.md)
    * Switching between languages
        * Per-cell
        * Within a single cell
    * [.NET variable sharing](variable-sharing.md)
    * [Accessing kernel variables from the client with JavaScript](javascript-overview.md) 

## Technical details

* [Architecture](kernels-overview.md)
* How Jupyter kernel installation works

## Visualization

* XPlot
* Visualization with JavaScript libraries

## .NET Interactive API Guides

* Using the .NET Interactive [command-line interface](../src/dotnet-interactive/CommandLine/readme.md)

### .NET API Guide ([TODO](https://github.com/dotnet/interactive/issues/815))

* Commands and events
* Formatter APIs
    * Working with MIME types 
* PocketView
* Magic commands
* Kernel APIs
    * Variable sharing
* JSON API for Standard I/O and HTTP modes ([TODO](https://github.com/dotnet/interactive/issues/813))

### JavaScript API Guide ([TODO](https://github.com/dotnet/interactive/issues/814))

* Variable access from the client
* RequireJS support
* Accessing static resources
* Sending kernel commands and consuming kernel events
 
## Extending .NET Interactive

* [Overview](extending-dotnet-interactive.md)
* [Building your own extension](extending-dotnet-interactive.md)
  * [Adding magic commands](extending-dotnet-interactive.md#adding-magic-commands)
* Publishing your extension using NuGet


