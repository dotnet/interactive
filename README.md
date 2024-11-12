# .NET Interactive

## What is .NET Interactive?

.NET Interactive is an engine and API for running and editing code interactively, including:

* Running code and getting its results.
* Evaluating code to provide language services such as completions and diagnostics.
* Sharing data and variables between multiple languages and across remote machines.

While typically associated with notebook technologies such as Jupyter and the [Polyglot Notebooks extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode), .NET Interactive has other uses as well, such as building REPLs and embedded script engines.

The following languages are supported by Polyglot Notebooks and .NET Interactive:

| Language                      | Variable sharing |
|-------------------------------|------------------|
| C#                            |        ✅       |
| F#                            |        ✅       |   
| PowerShell                    |        ✅       |          
| JavaScript                    |        ✅       |          
| SQL                           |        ✅       |   
| KQL ([Kusto Query Language](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/query/))    |        ✅       |  
| [Python](docs/jupyter-in-polyglot-notebooks.md)  |        ✅       |
| [R](docs/jupyter-in-polyglot-notebooks.md)       |        ✅       |      
| HTML                         |        ⛔         |     
| HTTP                         |        ✅         | 
| [Mermaid](https://mermaid.js.org/intro/)         |        ⛔       |        

### Polyglot Notebooks

Since .NET Interactive is capable of running as a kernel for notebooks, it enables a polyglot (multi-language) notebook experience. When using the .NET Interactive kernel, you can use different languages from one cell to the next, share variables between languages, and dynamically connect new languages and remote kernels within a notebook. There's no need to install different Jupyter kernels, use wrapper libraries, or install different tools to get the best experience for the language of your choice. You can always use the best language for the job and seamlessly transition between different stages of your workflow, all within one notebook.

For the best experience when working with multi-language notebooks, we recommend installing the [Polyglot Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) extension for Visual Studio Code. While the full .NET Interactive feature set is available in Jupyter, many features are only usable via code, whereas the Polyglot Notebooks extension provides additional features including a language/kernel picker for each cell, enhanced language services, a multi-kernel variable viewer, and more.

### Jupyter

The most popular notebook platform is Jupyter, and .NET Interactive is a fully supported Jupyter kernel that you can use with Jupyter Notebook, JupyterLab, nteract, and other Jupyter frontends. You can read more about using .NET Interactive as a Jupyter kernel [here](docs/NotebookswithJupyter.md).

### REPLs

.NET Interactive can be used as the execution engine for REPLs as well. The experimental [.NET REPL](https://github.com/jonsequitur/dotnet-repl) is one example of a command line REPL built on .NET Interactive. In addition, .NET REPL can be used for automated command line execution of notebooks.

### Small factor devices

.NET Interactive supports running on devices like Raspberry Pi and [pi-top [4]](https://github.com/pi-top/pi-top-4-.NET-Core-API). You can find instructions [here](docs/small-factor-devices.md).

## FAQ

For more information, please refer to our [FAQ](./docs/FAQ.md). 

## Acknowledgements 

The multi-language experience of .NET Interactive is truly a collaborative effort among different teams at Microsoft and in the community. We'd like to thank the following teams for contributing their time and expertise to helping bring support for other languages:

- **PowerShell Team:** PowerShell support
- **Azure Data Team:** SQL and KQL support
- **Azure Notebooks Team**: Python, R, and Jupyter subkernel support

## Telemetry

Telemetry is collected when the `dotnet-interactive` tool is started. (If you are using the .NET Interactive libraries directly, they do not emit telemetry.) Once `dotnet-interactive` is running, it emits the names of packages imported into the notebook and the languages used to run individual cells. This data is hashed, allowing us to count unique values, but the pre-hashed values cannot be obtained from the telemetry. We do not collect any additional code or clear text from cells. All telemetry is anonymous. In addition, `dotnet-interactive` reports the usage for a specific subset of the verbs in the .NET Interactive CLI. Those verbs are:

* `dotnet interactive jupyter`
* `dotnet interactive jupyter install`
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

To disable this message and the .NET Core welcome message, set the `DOTNET_INTERACTIVE_SKIP_FIRST_TIME_EXPERIENCE` environment variable to `true`. Note that this variable has no effect on telemetry opt out.

## Contribution Guidelines

You can contribute to .NET Interactive with issues and pull requests. Simply filing issues for problems you encounter is a great way to contribute. Contributing code improvements is greatly appreciated. You can read more about our contribution guidelines [here](CONTRIBUTING.md).

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow Microsoft's [Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general.aspx). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party’s policies.

