# Developer Instructions

## Building the `dotnet-interactive` tool and libraries

If you would like to build `dotnet-interactive` tool and its associated libraries, follow these steps.

### Prerequisites

This repo depends on symbolic links between directories. By default, Windows doesn't support this feature. To work around this scenario, please run the PowerShell script `<root>/src/ensure-symlinks.ps1` as an administrator. This usually only needs to be run once.

This project depends on .NET 6.0. Before working on the project, check that the .NET prerequisites have been met:

- [Prerequisites for .NET on Windows](https://docs.microsoft.com/en-us/dotnet/core/install/windows?tabs=net60#dependencies)
- [Prerequisites for .NET on Linux](https://docs.microsoft.com/en-us/dotnet/core/install/linux?tabs=net60#dependencies)
- [Prerequisites for .NET on macOS](https://docs.microsoft.com/en-us/dotnet/core/install/macos?tabs=net60#dependencies)

### Visual Studio / Visual Studio Code

This project supports [Visual Studio 2022](https://visualstudio.com) and [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/). Any version, including the free Community Edition, should be sufficient, as long as you install Visual Studio support for .NET development.

This project also supports using [Visual Studio Code](https://code.visualstudio.com). Install the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) and install the [.NET SDK](https://dotnet.microsoft.com/download) to get started.

### Build and test (command line)

You can also build this project on the command line by running the following scripts in the root of the repo:

Windows:

    > .\build.cmd

Linux or macOS:

    $ ./build.sh

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

### Install your local build

To build and then install your developer build of the `dotnet-interactive` global tool, you can run the PowerShell script

    pwsh src/dotnet-interactive/build-and-install-dotnet-interactive.ps1

Powershell for .NET Core is required. This will uninstall any previous version of `dotnet-interactive` you might have installed.

### Arcade build system

.NET Interactive is built with the support of the [Arcade](https://github.com/dotnet/arcade) build system. The Arcade tools provide common infrastructure for building, testing, and publishing .NET Foundation projects. This build system is not required for local development work, but using it will provide a higher fidelity

If you prefer a development environment that's more consistent with the out-of-the-box .NET experience, you can set the environment variable `DisableArcade` to `1`.

## Building the Visual Studio Code extension

In order to build the .NET Interactive Notebooks Visual Studio Code extension, please follow the instructions below. Note that it's not necessary to use a local build of `dotnet-interactive` in order to work on the Visual Studio Code extension.

### Prerequisites

To get started, you'll need:

1. [Visual Studio Code Insiders](https://code.visualstudio.com/insiders/).

2. [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).

3. The LTS version of [nodejs](https://nodejs.org/en/download/).

### Build and test

1. (*Windows only*) Open a PowerShell terminal as administrator and run `<REPO-ROOT>/src/ensure-symlinks.ps1`.

2. Follow the regular build instructions as given above.

3. Open the `<REPO-ROOT>/src/dotnet-interactive-vscode-insiders` directory in Visual Studio Code Insiders. (From your terminal, you can run `code-insiders <REPO-ROOT>/src/dotnet-interactive-vscode-insiders`.)

4. Make the desired source code changes.

5. Press F5 to launch the Visual Studio Code Extension Development Host.

6. Run `.NET Interactive: Create new blank notebook` or open a file with the `.ipynb` extension.

### Use a local build of the `dotnet-interactive` tool

If you've made changes to `dotnet-interactive` and want to try them out with Visual Studio Code, follow these steps:

1. Make appropriate changes and build the `<REPO-ROOT>/src/dotnet-interactive/dotnet-interactive.csproj` project.

2. In an instance of Visual Studio Code Insiders that has the extension installed (either via the marketplace or through the steps above), change the launch settings for the `dotnet-interactive` tool:

   a. Open the Command Palette (`Ctrl-Shift-P` on Windows or `Cmd-Shift-P` on macOS) and run `Preferences: Open Settings (Json)`.

   b. In the file that opens, add the following:

      ```js
        "dotnet-interactive.kernelTransportArgs": [
            "{dotnet_path}",
            "/PATH/TO/REPO/ROOT/artifacts/bin/dotnet-interactive/Debug/net6.0/Microsoft.DotNet.Interactive.App.dll",
            "[vscode]",
            "stdio",
            "--log-path",
            "/path/to/a/folder/for/your/logs/",
            "--verbose",
            "--working-dir",
            "{working_dir}"
        ],

        "dotnet-interactive.notebookParserArgs": [
            "{dotnet_path}",
            "/PATH/TO/REPO/ROOT/artifacts/bin/dotnet-interactive/Debug/net6.0/Microsoft.DotNet.Interactive.App.dll",
            "notebook-parser"
        ]
      ```

3. Save `settings.json`.

4. Restart VS Code.

5. To revert back to the original settings, delete the settings added in step 2 above and restart VS Code.

### Use a local build of a `dotnet-interactive` extension

If you've made changes to one of the `dotnet-interactive` extensions and want to use them locally, follow these steps:

1. Run `build.cmd -pack`/`./build.sh --pack` to create the Nuget packages for the extensions

2. Ensure that there aren't any kernels running for the extension in question. It's generally best to close all Notebooks opened in VS Code to accomplish this.

3. Run the `.NET Interactive: Create a new blank notebook` command in VS Code. Select `.dib` or `.ipynb` as the extension and any language as default.

4. Save the Notebook anywhere you like

5. Run the `.NET Interactive: Restart the current Notebook's kernel` command

6. In the first code cell, paste this code
   - In the FolderName, give the path to the nuget cache. This should be `%userprofile%\.nuget\packages` on Windows and `~/.nuget/packages` on Mac/Linux
   - Also replace `EXTENSIONNAME` with the name of the extension (e.g. `microsoft.dotnet.interactive.sqlserver`)
   - On the #i line, fill in the path to the `dotnet-interactive` repo root
   - On the #r line, use the same `EXTENSIONNAME` above, and then look in the `artifacts\packages\Debug\Shipping` folder for the package you're using and get the version number from the name. e.g. a package named `Microsoft.DotNet.Interactive.SqlServer.1.0.0-dev.nupkg` would result in this line `#r "nuget: Microsoft.DotNet.Interactive.SqlServer, 1.0.0-dev"`

```text
#!powershell

$FolderName = "\PATH\TO\NUGET\CACHE\packages\microsoft.dotnet.interactive.<EXTENSIONNAME>"
if (Test-Path $FolderName) {

    Remove-Item $FolderName -Recurse -Force
}
else
{
    Write-Host "Folder Doesn't Exist"
}

#!csharp

#i "nuget: \PATH\TO\REPO\ROOT\artifacts\packages\Debug\Shipping"

#r "nuget: Microsoft.DotNet.Interactive.<EXTENSIONNAME>, <EXTENSIONVERSION>"
```

7. Run the cell
   - If you get an error about access being denied, ensure that all other Notebooks are closed and then restart the kernel again as in step 5
8. Now, use the kernel as you normally would. You should see your local changes being used by the extension.
