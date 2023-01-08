# .NET Interactive Documentation 

_Our documentation is still a work in progress. There are a number of topics listed under [Features](#features) below that don't have outgoing links yet, but we've included them to give a better sense of the current feature set. Please open an issue if you'd like to know more about one of the topics we haven't gotten around to documenting yet._

## What is .NET Interactive?

.NET Interactive is an engine that can run multiple languages and share variables between them. Languages currently supported include: 

- C# 
- F#
- PowerShell
- JavaScript
- SQL 
- KQL (Kusto Query Language)
- HTML*
- Mermaid*

*Variable sharing not available

## What can .NET Interactive be used for? 

As a powerful and versatile engine, .NET Interactive can be used to create and power a number of tools and experiences such as: 

- Polyglot Notebooks
- REPLs
- Embeddable script enginges

### Polyglot Notebooks

Since .NET Interactive is capable of running as a kernel for notebooks, it enables a polyglot (multi-language) notebook experience. When using the .NET Interactive kernel, you can use different languages from one cell to the next, share variables between languages, and dynamically connect new languages and remote kernels within a notebook. There's no need to install different Jupyter kernels, use wrapper libraries, or install different tools to get the best experience for the language of your choice. You can always use the best language for the job and seamlessly transition between different stages of your workflow, all within one notebook.

For the best experience when working with multi-language notebooks, we recommend installing the [Polyglot Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) extension for Visual Studio Code. While the full .NET Interactive feature set is available in Jupyter, many features are only usable via code, whereas the Polyglot Notebooks extension provides additional features including a language/kernel picker for each cell, enhanced language services, a multi-kernel variable viewer, and more.

### Jupyter and nteract

There are several ways to get started using .NET Interactive with Jupyter, including Jupyter Notebook, JupyterLab, and nteract.

* [Try sample .NET notebooks online using Binder](NotebooksOnBinder.md).
* [Create and run .NET notebooks on your machine](NotebookswithJupyter.md).
* [Share .NET notebooks online using Binder](CreateBinder.md).

### REPLs

.NET Interactive can be used as the execution engine for REPLs as well. The experimental [.NET REPL](https://github.com/jonsequitur/dotnet-repl) is one example of a command line REPL built on .NET Interactive. In addition, .NET REPL can actually be used to set up automation for your Polyglot Notebooks. 

## Acknowledgements 

The multi-language experience of .NET Interactive is truly a collaborative effort amongst other groups at Microsoft. We'd like to thank the following teams for contributing their time and expertise to helping light up functionality for other languages. 

- **PowerShell Team:** PowerShell support
- **Azure Data Team:** SQL and KQL support

## Other

### Small factor devices

We support running on devices like Raspberry Pi and [pi-top [4]](https://github.com/pi-top/pi-top-4-.NET-Core-API). You can find instructions [here](small-factor-devices.md).

### Telemetry

Telemetry is collected when .NET Interactive is started. Once .NET Interactive is running, we collect hashed versions of packages imported into the notebook and the languages used to run individual cells. We do not collect any additional code or clear text from cells. The telemetry is anonymous and reports only the values for a specific subset of the verbs in the .NET Interactive CLI. Those verbs are:

* `dotnet interactive jupyter`
* `dotnet interactive jupyter install`
* `dotnet interactive http`
* `dotnet interactive stdio`

#### How to opt out

The .NET Interactive telemetry feature is enabled by default. To opt out of the telemetry feature, set the `DOTNET_INTERACTIVE_CLI_TELEMETRY_OPTOUT` environment variable to `1` or `true`.

#### Disclosure

The .NET Interactive tool displays text similar to the following when you first run one of the .NET Interactive CLI commands (for example, `dotnet interactive jupyter install`). Text may vary slightly depending on the version of the tool you're running. This "first run" experience is how Microsoft notifies you about data collection.

```console
Telemetry
---------
The .NET Core tools collect usage data in order to help us improve your experience.The data is anonymous and doesn't include command-line arguments. The data is collected by Microsoft and shared with the community. You can opt-out of telemetry by setting the DOTNET_INTERACTIVE_CLI_TELEMETRY_OPTOUT environment variable to '1' or 'true' using your favorite shell.
```

To disable this message and the .NET Core welcome message, set the `DOTNET_INTERACTIVE_CLI_TELEMETRY_OPTOUT` environment variable to `true`. Note that this variable has no effect on telemetry opt out.

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

* [Accessing kernel data from client-side JavaScript](javascript-overview.md#accessing-kernel-data-from-client-side-javascript-code)
* [Loading modules / RequireJS support](javascript-overview.md#loading-external-javascript-modules-at-runtime)
* Accessing static resources
* Sending kernel commands and consuming kernel events
 
## Extending .NET Interactive

* [Building your own extension](extending-dotnet-interactive.md)
  * [Adding magic commands](extending-dotnet-interactive.md#adding-magic-commands)
* Publishing your extension using NuGet
