# .NET Interactive

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
- Embeddable script engines

## Polyglot Notebooks

Since .NET Interactive is capable of behaving as a kernel for notebooks, it enables a polyglot (multi-language) notebook experience. 

In Polyglot Notebooks, you can use multiple languages and share variables between them. No more installing different Jupyter kernels, using wrapper libraries, or different tools to get the best language server support for the language of your choice. Always use the best language for the job and seamlessly transition between different states of your workflow, all within one notebook.

### Visual Studio Code

For the **best experience** when working with multi-language notebooks, we recommend working in VS Code and installing the [Polyglot Notebooks Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode).  

![SQLJavascript](https://user-images.githubusercontent.com/19276747/201805564-80243725-2ee4-49d5-89bd-88a01a373cad.gif)


### Jupyter and nteract

There are several ways to get started using .NET Interactive with Jupyter, including Jupyter Notebook, JupyterLab, and nteract.

* [Try sample notebooks online using Binder](docs/NotebooksOnBinder.md).
* [Create and run notebooks on your machine](docs/NotebookswithJupyter.md).
* [Share notebooks online using Binder](docs/CreateBinder.md).


### REPLs

.NET Interactive can be used as the execution engine for REPLs. For an example using a CLI, see [.NET REPL](https://github.com/jonsequitur/dotnet-repl). In addition, .NET REPL can actually be used to set up automation for your Polyglot Notebooks. 

### Small factor devices

We support running on devices like Raspberry Pi and [pi-top [4]](https://github.com/pi-top/pi-top-4-.NET-Core-API). You can find instructions [here](small-factor-devices.md).

## FAQ

For more information, please refer to our [FAQ](./docs/FAQ.md). 

## Acknowledgements 

The multi-language experience of .NET Interactive is truly a collaborative effort amongst other groups at Microsoft. We'd like to thank the following teams for contributing their time and expertise to helping light up functionality for other languages. 

- **PowerShell Team:** PowerShell
- **Azure Data/SQL Team:** SQL, KQL

## Telemetry

Telemetry is collected when .NET Interactive is started. Once .NET Interactive is running, we collect hashed versions of packages imported into the notebook and the languages used to run individual cells. We do not collect any additional code or clear text from cells. The telemetry is anonymous and reports only the values for a specific subset of the verbs in the .NET Interactive CLI. Those verbs are:

* `dotnet interactive jupyter`
* `dotnet interactive jupyter install`
* `dotnet interactive http`
* `dotnet interactive stdio`

### How to opt out

The .NET Interactive telemetry feature is enabled by default. To opt out of the telemetry feature, set the `DOTNET_INTERACTIVE_CLI_TELEMETRY_OPTOUT` environment variable to `1` or `true`.

### Disclosure

The .NET Interactive tool displays text similar to the following when you first run one of the .NET Interactive CLI commands (for example, `dotnet interactive jupyter install`). Text may vary slightly depending on the version of the tool you're running. This "first run" experience is how Microsoft notifies you about data collection.

```console
Telemetry
---------
The .NET Core tools collect usage data in order to help us improve your experience.The data is anonymous and doesn't include command-line arguments. The data is collected by Microsoft and shared with the community. You can opt-out of telemetry by setting the DOTNET_INTERACTIVE_CLI_TELEMETRY_OPTOUT environment variable to '1' or 'true' using your favorite shell.
```

We provide a number of packages that can be used to write custom [extensions](./docs/extending-dotnet-interactive.md) for .NET Interactive or to build your own interactive experiences.

To disable this message and the .NET Core welcome message, set the `DOTNET_INTERACTIVE_CLI_TELEMETRY_OPTOUT` environment variable to `true`. Note that this variable has no effect on telemetry opt out.

## Contribution Guidelines

You can contribute to .NET Interactive with issues and pull requests. Simply filing issues for problems you encounter is a great way to contribute. Contributing code improvements is greatly appreciated. You can read more about our contribution guidelines [here](CONTRIBUTING.md).





