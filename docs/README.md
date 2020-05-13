# .NET Interactive Documentation 

## Getting started 

### Jupyter and nteract

There are several ways to get started using .NET Interactive with Jupyter, including Jupyter Notebook, JupyterLab, and nteract.

* [Try sample .NET notebooks online using Binder](NotebooksOnBinder.md).
* [Create and run .NET notebooks on your machine](NotebooksLocalExperience.md).
* [Share .NET notebooks online using Binder](CreateBinder.md).

### Visual Studio Code

Work is underway to add support for the new Visual Studio Code [native notebook feature](https://code.visualstudio.com/updates/v1_45#_github-issue-notebook). While we are still in the early stages of this effort, if you'd like to experiment with it you can find instructions [here](../src/dotnet-interactive-vscode/README.md). 

### Telemetry

Telemetry is collected when various .NET Interactive command lines are run. Once .NET Interactive is running, we do not collect telemetry from any further user actions. The teletry is anonymous and collected only the values for a specific subset of the verbs on the .NET Interactive CLI. Those verbs are:

* `dotnet interactive jupyter`
* `dotnet interactive jupyter install`
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
* Plotting with [Xplot](https://fslab.org/XPlot/)
* Import NuGet packages 
* Language-specific features
    * C#
        * The [C# scripting](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) dialect
        * PocketView
    * PowerShell
        * PowerShell profile support
        * PowerShell host support 
        * AzShell support
    * F#
        * F# Interactive (FSI)
    * JavaScript
* ["Magic commands"](./magic-commands.md)
* Getting input from the user
* Multi-language notebooks
    * Switching between languages
        * Per-cell
        * Within a single cell
    * .NET variable sharing
    * Accessing kernel variables from the client with JavaScript 

## Technical details

* Architecture
* How Jupyter kernel installation works

## Visualization

* XPlot
* Visualization with JavaScript libraries

## .NET Interactive API Guides

* Using the .NET Interactive [command-line interface](../src/dotnet-interactive/CommandLine/readme.md)

### .NET API Guide

* Commands and events
* Formatter APIS 
    * Working with MIME types 
* PocketView
* Magic commands
* Kernel APIs
    * Variable sharing
* JSON API for Standard I/O and HTTP modes

### JavaScript API Guide

* Variable access from the client
* RequireJS support
 
## [Extending .NET Interactive]

* [Overview](extensions-overview.md)
* Adding magic commands
* Building your own extension
* Publishing your extension 


