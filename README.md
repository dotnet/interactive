# .NET Interactive <img src ="https://user-images.githubusercontent.com/2546640/56708992-deee8780-66ec-11e9-9991-eb85abb1d10a.png" width="80px" alt="dotnet bot in space" align ="right">

[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/dotnet/interactive/master?urlpath=lab) [![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/interactive/interactive-ci?branchName=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=744&branchName=master)

.NET Interactive gives developers, data scientists, makers, and learners tools to write code and see the results immediately. You can explore data, experiment with code, and try new ideas. Use .NET Interactive to build .NET Jupyter notebooks or custom interactive coding experiences including bots and custom REPLs.

## Jupyter Notebooks with .NET <img src ="https://upload.wikimedia.org/wikipedia/commons/thumb/3/38/Jupyter_logo.svg/207px-Jupyter_logo.svg.png" height="1.5em" alt="Project Jupyter" >

[Project Jupyter](https://jupyter.org/) is a popular platform for creating interactive notebooks that can be used for data science, documentation, DevOps, and much more.

<img src = "https://user-images.githubusercontent.com/547415/78056370-ddd0cc00-7339-11ea-9379-c40f8b5c1ae5.png" width = "70%">
<img src="https://user-images.githubusercontent.com/2546640/67912370-1b99b080-fb60-11e9-9839-0058d02488cf.png" width="62%">

# Jupyter Notebooks with .NET Core | Preview 2 

There are several ways to get started using .NET with Jupyter.

- [Try sample .NET notebooks online using Binder](docs/NotebooksOnBinder.md). This also allows you try out our daily builds, which includes preview features of F# 5.
- [Create and run .NET notebooks on your machine](docs/NotebooksLocalExperience.md). (Installation instructions [below](#Install).)
- [Share your own .NET notebooks with others online using Binder](docs/CreateBinder.md).
- [Use .NET Interactive with nteract](https://nteract.io/kernels/dotnet)

## Packages

We provide a number of packages that can be used to write custom extensions for .NET Interactive or to build your own interactive experiences.


Package                                    | Version                                                                                                                                                         | Description
-------------------------------------------| ----------------------------------------------------------------------------------------------------------------------------------------------------------------| ------------
`Microsoft.dotnet-interactive`             | [![Nuget](https://img.shields.io/nuget/v/Microsoft.dotnet-interactive.svg)](https://www.nuget.org/packages/Microsoft.dotnet-interactive)                        | The `dotnet-interactive` global tool
`Microsoft.DotNet.Interactive`             | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive)                        | Core types for building applications providing interactive programming for .NET.
`Microsoft.DotNet.Interactive.Formatting`  | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.Formatting.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.Formatting)  | Convention-based and highly configurable .NET object formatting for interactive programming, including support for mime types suitable for building visualizations for Jupyter Notebooks and web browsers.
`Microsoft.DotNet.Interactive.FSharp`      | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.FSharp.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.FSharp)          | Microsoft.DotNet.Interactive.IKernel implementation for F#
`Microsoft.DotNet.Interactive.CSharp`      | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.CSharp.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.CSharp)          | Microsoft.DotNet.Interactive.IKernel implementation for C#

## Contribution Guidelines

You can contribute to .NET Interactive with issues and PRs. Simply filing issues for problems you encounter is a great way to contribute. Contributing code improvements is greatly appreciated. You can read more about our contribution guidelines [here](https://github.com/dotnet/runtime/blob/master/CONTRIBUTING.md).

## Customers & Partners

|    [Azure Synapse Analytics ](https://azure.microsoft.com/en-us/services/synapse-analytics/)   |Azure HDInsight (HDI)  |
|:-------------:|:-------------:|
| Azure Synapse Analytics uses the .NET kernel to write and run quick ad-hoc queries in addition to developing complete, end-to-end big data scenarios, such as reading in data, transforming it, and visualizing it|You can launch Jupyter notebooks from your HDInsight cluster to run big data queries against the compute resources in that cluster. |


