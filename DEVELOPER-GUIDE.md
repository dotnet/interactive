# Developer Instructions

# Building the `dotnet-interactive` tool and libraries

If you would like to build `dotnet-interactive` tool and its associated libraries, follow these steps.

## Prerequisites

This project depends on .NET 5.0. Before working on the project, check that the .NET prerequisites have been met:

   - [Prerequisites for .NET on Windows](https://docs.microsoft.com/en-us/dotnet/core/install/windows?tabs=net50#dependencies)
   - [Prerequisites for .NET on Linux](https://docs.microsoft.com/en-us/dotnet/core/install/linux?tabs=net50#dependencies)
   - [Prerequisites for .NET on macOS](https://docs.microsoft.com/en-us/dotnet/core/install/macos?tabs=net50#dependencies)

## Visual Studio / Visual Studio Code

This project supports [Visual Studio 2019](https://visualstudio.com) and [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/). Any version, including the free Community Edition, should be sufficient, as long as you install Visual Studio support for .NET development.

This project also supports using [Visual Studio Code](https://code.visualstudio.com). Install the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) and install the [.NET SDK](https://dotnet.microsoft.com/download) to get started.

## Build and test (command line)

You can also build this project on the command line by running the following scripts in the root of the repo:

Windows:

    > .\build.cmd -test

Linux or macOS:

    $ ./build.sh --test

You can both build and run the tests for the project by running the scripts with the following option:

Windows:

    > .\build.cmd -test

Linux or macOS:

    $ ./build.sh --test

For additional options, you can get help as follows:

Windows:

    > .\build.cmd -help

Linux or macOS:

    $ ./build.sh --help

## Install your local build

To build and then install your developer build of the `dotnet-interactive` global tool, you can run the PowerShell script

    pwsh src/dotnet-interactive/build-and-install-dotnet-interactive.ps1

Powershell for .NET Core is required. This will uninstall any previous version of `dotnet-interactive` you might have installed.

## Arcade build system

.NET Interactive is built with the support of the [Arcade](https://github.com/dotnet/arcade) build system. The Arcade tools provide common infrastructure for building, testing, and publishing .NET Foundation projects. This build system is not required for local development work, but using it will provide a higher-fidelity 

If you prefer a development environment that's more consistent with the out-of-the-box .NET experience, you can set the environment variable `DisableArcade` to `1`. 

# Building the Visual Studio Code extension

In order to build the .NET Interactive Notebooks Visual Studio Code extension, please follow the instructions below. Note that it's not necessary to use a local build of `dotnet-interactive` in order to work on the Visual Studio Code extension.

## Prerequisites

To get started, you'll need:

1. [Visual Studio Code Insiders](https://code.visualstudio.com/insiders/).

2. The LTS version of [nodejs](https://nodejs.org/en/download/).

## Build and test

1. Run `npm i` in the `<REPO-ROOT>/src/dotnet-interactive-vscode/` directory.

2. Open the `<REPO-ROOT>/src/dotnet-interactive-vscode/` directory in Visual Studio Code Insiders. (From your terminal, you can run `code-insiders <REPO-ROOT>/src/dotnet-interactive-vscode/`.)

3. Make appropriate changes to the `<REPO-ROOT>/src/` directory.

4. Press F5 to launch the Visual Studio Code Extension Development Host.

5. Open or create a file with a `.dib` extension

    OR 

   Open a Jupyter notebook using the VS Code command *Convert Jupyter notebook (.ipynb) to .NET Interactive notebook*.

    ![image](https://user-images.githubusercontent.com/547415/84576252-147a8800-ad68-11ea-8315-07757291710f.png)

## Use a local build of the `dotnet-interactive` tool 

If you've made changes to `dotnet-interactive` and want to try them out with Visual Studio Code, follow these steps:

1. Make appropriate changes and build the `<REPO-ROOT>/src/dotnet-interactive/dotnet-interactive.csproj` project.

2. In an instance of Visual Studio Code Insiders that has the extension installed (either via the marketplace or through the steps above), change the launch settings for the `dotnet-interactive` tool:

   1. Open the settings via `File` -> `Preferences` -> `Settings`, or by pressing `Ctrl + ,`.

   2. Filter the list by typing `dotnet-interactive`

   3. Find the setting labelled: `Dotnet-interactive: Kernel transport args`.

   4. Click `Edit in settings.json`.

   5. In the file that opens, make the following changes, updating the path where appropriate:

      ``` diff
       "dotnet-interactive.kernelTransportArgs": [
         "{dotnet_path}",
      -  "tool",
      -  "run",
      -  "dotnet-interactive",
      -  "--",
      +  "/PATH/TO/REPO/ROOT/artifacts/bin/dotnet-interactive/Debug/net5.0/Microsoft.DotNet.Interactive.App.dll",
         "[vscode]",
         "stdio",
         "--working-dir",
         "{working_dir}"
       ]
      ```

3. Save `settings.json`.

4. Any subsequently opened notebooks will use your local changes.

5. To revert back to the original settings, follow steps 1-3 then next to the text `Dotnet-interactive: Kernel transport args`, click the gear icon then `Reset Setting`.
