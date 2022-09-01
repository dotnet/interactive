
# .NET Interactive <img src ="https://user-images.githubusercontent.com/2546640/56708992-deee8780-66ec-11e9-9991-eb85abb1d10a.png" width="80px" alt="dotnet bot in space" align ="right">

[![Discord](https://img.shields.io/discord/732297728826277939?label=discord)](https://discord.gg/3pvut9YujN) [![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/interactive/interactive-ci?branchName=main)](https://dev.azure.com/dnceng-public/public/_build?definitionId=71&branchName=main) [![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/dotnet/interactive/main?urlpath=lab) 

.NET Interactive takes the power of .NET and embeds it into *your* interactive experiences. Share code, explore data, write, and learn across your apps in ways you couldn't before.

* [Notebooks](#notebooks-with-net): Jupyter, nteract, and Visual Studio Code 
* [Code bots](https://github.com/CodeConversations/CodeConversations)
* Devices like [Raspberry Pi](https://www.raspberrypi.org/)
* Embeddable script engines
* [REPLs](https://github.com/jonsequitur/dotnet-repl)

*.NET Interactive IS .NET UNLEASHED*

# Notebooks with .NET

## Visual Studio Code

We recently introduced the [.NET Interactive Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) extension for Visual Studio Code, which adds support for .NET Interactive using the new Visual Studio Code [native notebook feature](https://code.visualstudio.com/updates/v1_45#_github-issue-notebook). We encourage you to [try it out](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode). If you'd like to contribute, you can start [here](CONTRIBUTING.md). 

![newnotebook](https://user-images.githubusercontent.com/2546640/130660742-deb5c33b-020d-4d03-8034-7f11532c3201.gif)

---

## Features
### Multi-language notebooks 
.NET Interactive enables users to mix languages in a single notebook or cell without a wrapper. The multi-language experience opens up doors for users to use the best language for the task at hand.

**Languages supported**
- C# and F# 
- PowerShell built in collaboration with the PowerShell team 💙
- JavaScript
- HTML 
- SQL built in collaboration with the  Azure Data / SQL team 💙

**Coming soon**
- Kusto 

**What languages are we exploring?** 
- Python and R 

### Variable Sharing 

.NET Interactive enables you to write code in multiple languages within a single notebook and in order to take advantage of those languages' different strengths, you might find it useful to share data between them. Read more [here](https://github.com/dotnet/interactive/blob/main/docs/variable-sharing.md).

![Notebooks-variable-sharing](https://user-images.githubusercontent.com/2546640/130664292-1cdfb806-a6f6-4874-bcad-a5eb4517a925.gif)

The gif above showcases the following:
- Variable sharing across C#, HTML and JavaScript cells.
- Multi-language cells.

For more examples on multi-language notebooks and variable sharing check out our [polyglot samples](https://github.com/dotnet/interactive/tree/main/samples/notebooks/polyglot).

### Visualization 

**Low code visualization**

In just a single line of code easily visualize data with Microsoft SandDance and [nteract DataExplorer](https://data-explorer.nteract.io/). For  example the code snippet below will render an interactive [Microsoft SandDance](https://www.microsoft.com/en-us/research/project/sanddance/).
```csharp
housingData.ExploreWithSandDance().Display();
```

![low-code-visualization](https://user-images.githubusercontent.com/2546640/130510820-6a5b5f9d-a0cc-4fef-8a3d-ea741a30d7f8.gif)

For more [low code visualization](https://github.com/dotnet/interactive/tree/main/samples/ExtensionLab) examples, check out our samples.

**Works with your favorite visualization libraries**

![d3js](https://user-images.githubusercontent.com/2546640/130669124-09f11de8-e324-4c2e-bdbc-c49fd85511c2.gif)

The image below showcases the following: 

- `C#` cell: Define a variable in C#
- `JavaScript` cell: Use RequireJS to import d3.js
- `HTML cell`:  Visualize the data

Full example [here](https://github.com/dotnet/interactive/blob/main/samples/notebooks/polyglot/d3js.ipynb).

----

## Jupyter and nteract

[Project Jupyter](https://jupyter.org/) is a popular platform for creating interactive notebooks that can be used for data science, documentation, DevOps, and much more.

<img src="https://user-images.githubusercontent.com/547415/78056370-ddd0cc00-7339-11ea-9379-c40f8b5c1ae5.png" width="70%">
<img src="https://user-images.githubusercontent.com/2546640/67912370-1b99b080-fb60-11e9-9839-0058d02488cf.png" width="70%">

There are several ways to get started using .NET with Jupyter, including Jupyter Notebook, JupyterLab, and nteract.

- [Try sample .NET notebooks online using Binder](docs/NotebooksOnBinder.md). This also allows you try out our daily builds, which include preview features of F# 5.
- [Install .NET Interactive](docs/NotebookswithJupyter.md) to create and run .NET notebooks on your machine.
- [Share your own .NET notebooks with others online using Binder](docs/CreateBinder.md).
- [Use .NET Interactive with nteract](https://nteract.io/kernels/dotnet)
- [Use .NET Interactive on Raspberry Pi and pi-top](docs/small-factor-devices.md)

## Documentation

You can find additional documentation [here](./docs/README.md).

## Packages

We provide a number of packages that can be used to write custom [extensions](./docs/extending-dotnet-interactive.md) for .NET Interactive or to build your own interactive experiences.

Package                                    | Version                                                                                                                                                         | Description
:------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------
`Microsoft.dotnet-interactive`             | [![Nuget](https://img.shields.io/nuget/v/Microsoft.dotnet-interactive.svg)](https://www.nuget.org/packages/Microsoft.dotnet-interactive)                        | The `dotnet-interactive` global tool
`Microsoft.DotNet.Interactive`             | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive)                        | Core types for building applications providing interactive programming for .NET.
`Microsoft.DotNet.Interactive.Formatting`  | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.Formatting.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.Formatting)  | Convention-based and highly configurable .NET object formatting for interactive programming, including support for mime types suitable for building visualizations for Jupyter Notebooks and web browsers.
`Microsoft.DotNet.Interactive.FSharp`      | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.FSharp.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.FSharp)          | Microsoft.DotNet.Interactive.Kernel implementation for F#
`Microsoft.DotNet.Interactive.CSharp`      | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.CSharp.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.CSharp)          | Microsoft.DotNet.Interactive.Kernel implementation for C#
`Microsoft.DotNet.Interactive.PowerShell`      | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.PowerShell.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.PowerShell)          | Microsoft.DotNet.Interactive.Kernel implementation for PowerShell

## Contribution Guidelines

You can contribute to .NET Interactive with issues and pull requests. Simply filing issues for problems you encounter is a great way to contribute. Contributing code improvements is greatly appreciated. You can read more about our contribution guidelines [here](CONTRIBUTING.md).

## Customers & Partners

|    [Azure Synapse Analytics ](https://azure.microsoft.com/en-us/services/synapse-analytics/)   |Azure HDInsight (HDI)  |
|:-------------:|:-------------:|
| Azure Synapse Analytics uses the .NET kernel to write and run quick ad-hoc queries in addition to developing complete, end-to-end big data scenarios, such as reading in data, transforming it, and visualizing it|You can launch Jupyter notebooks from your HDInsight cluster to run big data queries against the compute resources in that cluster. 


