# Polyglot Notebooks

The [Polyglot Notebooks extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode), powered by [.NET Interactive](https://github.com/dotnet/interactive), brings support for multi-language notebooks to Visual Studio Code. Classic notebook software typically supports notebooks that use only one language at a time. With Polyglot Notebooks, features such as completions, documentation, syntax highlighting, and diagnostics are available for many languages in one notebook. In addition, different cells in the same notebook can run in separate processes or on different machines, allowing a notebook to span local and cloud environments in one combined workflow.

## Fully Interoperable with Jupyter

Polyglot Notebooks are fully interoperable with Jupyter and support the `.ipynb` file extension. You don't need to choose between the capabilities of Polyglot Notebooks and the rich Jupyter ecosystem. If your notebook is saved in the `.ipynb` format, you can open it in Jupyter and the cell languages will still be recognized. When working in Jupyter using the .NET Interactive kernel, you can switch cell languages using magic commands. 

## Supported Languages

The following languages are supported by Polyglot Notebooks:

| Language                                    | Variable sharing supported |
|---------------------------------------------|-------------------------------------------------------|
| C#                                          |        ✅                 |
| F#                                          |        ✅                 |   
| PowerShell                                  |        ✅                 |          
| JavaScript                                  |        ✅                 |          
| SQL                                         |        ✅                 |   
| KQL ([Kusto Query Language](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/query/))    |        ✅       |       
| HTML                                        |        ⛔                 |     
| [Mermaid](https://mermaid.js.org/intro/)    |        ⛔                 |        
| Python (Preview)                            |        ✅                 |          
| R (Preview)                                 |        ✅                 |          
  
## Features

- Run and execute code for all featured languages
- Share variables between languages
- Connect to and query Microsoft SQL Server
- Connect to and query Kusto clusters
- Language server support such as completions, syntax highlighting, signature help, and diagnostics for each language
- See the state of all variables using the Variables View
- Create detailed diagrams and visualizations using [Mermaid](https://mermaid-js.github.io/mermaid/#/)
- Integrate with your favorite VS Code extensions such as [VIM](https://marketplace.visualstudio.com/items?itemName=vscodevim.vim) and [GitLens](https://marketplace.visualstudio.com/items?itemName=eamodio.gitlens)
- Notebook-friendly diffing tool that makes it easy to visually compare inputs, outputs, and metadata
- Navigate via Outline View
- Customizable notebook layout
- Connect to Python (3.7+) and R Jupyter kernels installed locally or remotely and share variables between them.

## Getting Started

1.  Install the latest [Visual Studio Code](https://code.visualstudio.com/).
2.  Install the latest [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download).
3.  Install the Polyglot Notebooks extension from the [marketplace](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode).

## Creating Notebooks

To create a new polyglot notebook, open the Command Palette(`Ctrl+Shift+P`) on Windows or (`Cmd+Shift+P`) on MacOS, and select **Polyglot Notebook: Create new blank notebook**. You can also create a new notebook with `Ctrl+Shift+Alt+N` key combination on Windows. 

## Running Code

Execute code by selecting the cell kernel in the bottom right of each cell, or use language magic commands to mix languages in one cell regardless of the chosen cell kernel. 

![Running Code](https://github.com/dotnet/interactive/raw/main/images/RunningCodeExample.gif)

## Variable Sharing and Variables View 

Share variables between languages using the `#!set` magic command. If you can't remember the syntax, you can always generate it using the `Share` action in the Polyglot Notebooks Variables View. 

![Variable Sharing with the Variables View](https://github.com/dotnet/interactive/raw/main/images/variable-sharing-with-variables-view.gif)

## Examples 

 - Connect to a SQL database, share query results with JavaScript, and create your own custom visualizations.

![SQL and JavaScript Example](https://github.com/dotnet/interactive/raw/main/images/SQLJavaScript.gif)

 - Create powerful diagrams and visualizations using code and text using [Mermaid](https://mermaid-js.github.io/mermaid/#/).

![Mermaid Example](https://github.com/dotnet/interactive/raw/main/images/MermaidExample.gif)

## Why do I need the .NET SDK? 

Polyglot Notebooks is powered by .NET Interactive, an engine that can connect multiple kernels and share variables between them, which is built using .NET technology. At this time, it is required for the extension to function.

## Filing Issues and Feature Requests

You can file issues or feature requests on the [.NET Interactive](https://github.com/dotnet/interactive/issues/new/choose) GitHub repository. 

## Telemetry

The Polyglot Notebooks extension for VS Code uses the `dotnet-interactive` tool which collects usage and sends telemetry to Microsoft to help us improve our products and services. 

Telemetry is collected when .NET Interactive is started. Once .NET Interactive is running, we collect hashed versions of packages imported into the notebook and the languages used to run individual cells. We do not collect any additional code or clear text from cells. The telemetry is anonymous and reports only the values for a specific subset of the verbs in the .NET Interactive CLI. Those verbs are:

* `dotnet interactive jupyter`
* `dotnet interactive jupyter install`
* `dotnet interactive stdio`

Read our [privacy statement](https://go.microsoft.com/fwlink/?LinkId=521839) to learn more.  See [here](https://github.com/dotnet/interactive/tree/main/docs#telemetry) to learn more about telemetry in Polyglot Notebooks. 

## License

Copyright © .NET Foundation, and contributors.

The source code to this extension is available on [https://github.com/dotnet/interactive](https://github.com/dotnet/interactive) and licensed under the [MIT license](https://github.com/dotnet/interactive/blob/main/License.txt).
