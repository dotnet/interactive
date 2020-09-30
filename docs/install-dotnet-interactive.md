# Installing .NET Interactive

The .NET Interactive tool works with multiple frontends, and so depending on how you plan to use it, there are different ways you can install it.

## Jupyter

.NET Interactive is a Jupyter kernel. To install it for use with Jupyter (including Jupyter Notebook, JupyterLab, nteract, Azure Data Studio, and others), follow the instructions [here](NotebooksLocalExperience.md). 

## Visual Studio Code

If you want a more lightweight installation and don't need Jupyter or Python, you can install the [.NET Interactive Notebooks extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode). 

*Note: When installing for Jupyter, the `dotnet-interactive` tool is installed as a [global tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools#install-a-global-tool), but the Visual Studio Code extension instals a separate instance as a [local tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools#install-a-local-tool), which it manages for you.*

## Standalone (No GUI)

.NET Interactive is a .NET Core tool and you can install it by itself. You might do this if you want to provide your own user interface.

## Embedding .NET Interactive using NuGet packages

We provide a number of packages that can be used to write custom [extensions](./docs/extending-dotnet-interactive.md) for .NET Interactive or to build your own interactive experiences.

Package                                    | Version                                                                                                                                                         | Description
:------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------
`Microsoft.dotnet-interactive`             | [![Nuget](https://img.shields.io/nuget/v/Microsoft.dotnet-interactive.svg)](https://www.nuget.org/packages/Microsoft.dotnet-interactive)                        | The `dotnet-interactive` global tool
`Microsoft.DotNet.Interactive`             | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive)                        | Core types for building applications providing interactive programming for .NET.
`Microsoft.DotNet.Interactive.Formatting`  | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.Formatting.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.Formatting)  | Convention-based and highly configurable .NET object formatting for interactive programming, including support for mime types suitable for building visualizations for Jupyter Notebooks and web browsers.
`Microsoft.DotNet.Interactive.CSharp`      | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.CSharp.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.CSharp)          | Microsoft.DotNet.Interactive.Kernel implementation for C#
`Microsoft.DotNet.Interactive.FSharp`      | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.FSharp.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.FSharp)          | Microsoft.DotNet.Interactive.Kernel implementation for F#
`Microsoft.DotNet.Interactive.PowerShell`      | [![Nuget](https://img.shields.io/nuget/v/Microsoft.DotNet.Interactive.PowerShell.svg)](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.FSharp)          | Microsoft.DotNet.Interactive.Kernel implementation for PowerShell
