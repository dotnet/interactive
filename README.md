# .NET Interactive <img src ="https://user-images.githubusercontent.com/2546640/56708992-deee8780-66ec-11e9-9991-eb85abb1d10a.png" width="80px" alt="dotnet bot in space" align ="right">
||[**Table of contents**](#table-of-contents) || [**Install**](#Install) || [**Customers & Partners**](#customers--partners) || [**Contribution Guidelines**](#contribution-guidelines) ||

[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/dotnet/interactive/master?urlpath=lab)


[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/interactive/interactive-ci?branchName=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=744&branchName=master)

## Welcome to the .NET Interactive repo.

 .NET interactive provides data Scientists and developers a way to explore data, experiment code, and try new ideas effortlessly.  Use .NET interactive to build .NET Jupyter notebooks. 

### Jupyter Notebooks with .NET

<img src="https://user-images.githubusercontent.com/2546640/72949473-60477900-3d56-11ea-8bc4-47352a613b78.png" width="80%">
<img src="https://user-images.githubusercontent.com/2546640/67912370-1b99b080-fb60-11e9-9839-0058d02488cf.png" width="62%">

## Table of Contents

# Jupyter Notebooks with .NET Core | Preview 2 <img src ="https://upload.wikimedia.org/wikipedia/commons/thumb/3/38/Jupyter_logo.svg/207px-Jupyter_logo.svg.png" width="38px" alt="dotnet bot in space" align ="right">

There are several ways to get started using .NET with Jupyter.

- [Try sample .NET notebooks online using Binder](docs/NotebooksOnBinder.md). This also allows you try out our daily builds.
- [Create and run .NET notebooks on your machine](docs/NotebooksLocalExperience.md).
   - Install instructions [below](#Install)
- [Share your own .NET notebooks with others online using Binder](docs/CreateBinder.md).
- [.NET Interactive with nteract](https://nteract.io/kernels/dotnet)

## Install
### How Install .NET Interactive 
First, make sure you have the following installed:

- The [.NET 3.0 SDK](https://dotnet.microsoft.com/download).
- **Jupyter**. Jupyter can be installed using [Anaconda](https://www.anaconda.com/distribution).

- Open the Anaconda Prompt (Windows) or Terminal (macOS) and verify that Jupyter is installed and present on the path:

```console
> jupyter kernelspec list
  python3        ~\jupyter\kernels\python3
```

- Next, in an ordinary console, install the `dotnet interactive` global tool:

```console
> dotnet tool install -g --add-source "https://dotnet.myget.org/F/dotnet-try/api/v3/index.json" Microsoft.dotnet-interactive
```

- Install the .NET kernel by running the following within your Anaconda Prompt:

```console
> dotnet interactive jupyter install

[InstallKernelSpec] Installed kernelspec .net-powershell in ~\jupyter\kernels\.net-powershell
.NET kernel installation succeeded

[InstallKernelSpec] Installed kernelspec .net-csharp in ~\jupyter\kernels\.net-csharp
.NET kernel installation succeeded

[InstallKernelSpec] Installed kernelspec .net-fsharp in ~\jupyter\kernels\.net-fsharp
.NET kernel installation succeeded
```
    
- You can verify the installation by running the following again in the Anaconda Prompt:

```console
> jupyter kernelspec list
  .net-csharp       ~\jupyter\kernels\.net-csharp
  .net-fsharp       ~\jupyter\kernels\.net-fsharp
  .net-powershell   ~\jupyter\kernels\.net-powershell
  python3           ~\jupyter\kernels\python3
```
For more detailed documentation visit see [table of content](#table-of-contents).
## Contribution Guidelines

As we are still in the early stages of our development, we are unable to take any feature PRs at the moment, but we intend to do so in the future.
If you find an issue or have a feature suggestion, please open an [issue](https://github.com/dotnet/interactive/issues/new/choose). And if you have any feature suggestions, please submit them using the "community suggestions" label.

## Customers & Partners
|    [Azure Synapse Analytics ](https://azure.microsoft.com/en-us/services/synapse-analytics/)   |Azure HDInsight (HDI)  |
|:-------------:|:-------------:|
| Azure Synapse Analytics uses the .NET kernel to write and run quick ad-hoc queries in addition to developing complete, end-to-end big data scenarios, such as reading in data, transforming it, and visualizing it|You can launch Jupyter notebooks from your HDInsight cluster to run big data queries against the compute resources in that cluster. |



