# Developer Instructions

## Building the `dotnet-interactive` tool and libraries

If you would like to build `dotnet-interactive` tool and its associated libraries, follow these steps.

### Prerequisites

- Windows
  - Enable [developer mode](https://docs.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development).
  - This repo depends on symbolic links between directories. By default, Windows doesn't support this feature. To work around this scenario, please run the PowerShell script `<root>/src/ensure-symlinks.ps1` as an administrator. This usually only needs to be run once.

    **N.b., using symbolic links in Windows requires the drive be formatted as NTFS.**

This project depends on .NET 9.0. Before working on the project, check that the .NET prerequisites have been met:

- [Prerequisites for .NET on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows)
- [Prerequisites for .NET on Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux)
- [Prerequisites for .NET on macOS](https://learn.microsoft.com/en-us/dotnet/core/install/macos)

### First build

The first build must be executed on the command line (see below for details), but subsequent builds can be done directly within Visual Studio / Visual Studio Code.

### Visual Studio / Visual Studio Code

This project supports [Visual Studio 2022](https://visualstudio.com) and [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/). Any version, including the free Community Edition, should be sufficient, as long as you install Visual Studio support for .NET development.

This project also supports using [Visual Studio Code](https://code.visualstudio.com). Install the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) and install the [.NET SDK](https://dotnet.microsoft.com/download) to get started.

### Build and test (command line)

You can also build this project on the command line by running the following scripts in the root of the repo:

Windows:

```console
    > .\build.cmd
```

Linux or macOS:

```console
    $ ./build.sh
```

You can both build and run the tests for the project by running the scripts with the following option:

Windows:

```console
    > .\build.cmd -test
```

Linux or macOS:

```console
    $ ./build.sh --test
```

For additional options, you can get help as follows:

Windows:

```console
    > .\build.cmd -help
```

Linux or macOS:

```console
    $ ./build.sh --help
```

### Install your local build

To build and then install your developer build of the `dotnet-interactive` tool, you can run the PowerShell script

```console
    pwsh src/dotnet-interactive/build-and-install-dotnet-interactive.ps1
```

PowerShell for .NET Core is required. This will uninstall any previous version of `dotnet-interactive` you might have installed.

### Arcade build system

.NET Interactive is built with the support of the [Arcade](https://github.com/dotnet/arcade) build system. The Arcade tools provide common infrastructure for building, testing, and publishing .NET Foundation projects. This build system is not required for local development work, but using it will provide a higher fidelity

If you prefer a development environment that's more consistent with the out-of-the-box .NET experience, you can set the environment variable `DisableArcade` to `1`.

## Building the Polyglot Notebooks extension for Visual Studio Code

In order to build the Polyglot Notebooks extension for Visual Studio Code, please follow the instructions below. Note that it's not necessary to use a local build of `dotnet-interactive` in order to work on the Visual Studio Code extension.

### Prerequisites

To get started, you'll need:

1. [Visual Studio Code Insiders](https://code.visualstudio.com/insiders/).

2. [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download).

3. The LTS version of [nodejs](https://nodejs.org/en/download/).

### Build and test

1. (*Windows only*) Open a PowerShell terminal as administrator and run `<REPO-ROOT>/src/ensure-symlinks.ps1`.

2. Follow the regular build instructions as given above.

3. Open the `<REPO-ROOT>/src/polyglot-notebooks-vscode-insiders` directory in Visual Studio Code Insiders. (From your terminal, you can run `code-insiders <REPO-ROOT>/src/polyglot-notebooks-vscode-insiders`.)

4. Make the desired source code changes.

5. Press F5 to launch the Visual Studio Code Extension Development Host.

6. Run `Polyglot Notebook: Create new blank notebook` or open a file with the `.ipynb` extension.

### Use a local build of the `dotnet-interactive` tool

If you've made changes to `dotnet-interactive` and want to try them out with Visual Studio Code, follow these steps:

1. Make appropriate changes and build the `<REPO-ROOT>/src/dotnet-interactive/dotnet-interactive.csproj` project.

2. In an instance of Visual Studio Code Insiders that has the extension installed (either via the marketplace or through the steps above), change the launch settings for the `dotnet-interactive` tool:

   a. Open the Command Palette (`Ctrl-Shift-P` on Windows or `Cmd-Shift-P` on macOS) and run `Preferences: Open Settings (Json)`.

   b. In the file that opens, add the following:

      ```json
        "dotnet-interactive.kernelTransportArgs": [
            "{dotnet_path}",
            "/PATH/TO/REPO/ROOT/artifacts/bin/dotnet-interactive/Debug/net9.0/Microsoft.DotNet.Interactive.App.dll",
            "[vscode]",
            "stdio",
            "--log-path",
            "/path/to/a/folder/for/your/kernel-logs/",
            "--verbose",
            "--working-dir",
            "{working_dir}"
        ],

        "dotnet-interactive.notebookParserArgs": [
            "{dotnet_path}",
            "/PATH/TO/REPO/ROOT/artifacts/bin/dotnet-interactive/Debug/net9.0/Microsoft.DotNet.Interactive.App.dll",
            "notebook-parser",
            "--log-path",
            "/path/to/a/folder/for/your/parser-logs/",
        ]
      ```

3. Save `settings.json`.

4. Restart VS Code.

5. To revert back to the original settings, delete the settings added in step 2 above and restart VS Code.

### Use a local build of a Polyglot Notebooks extension

If you've made changes to the Polyglot Notebooks extension and want to try your changes locally, follow these steps:

1. Run `build.cmd -pack`/`./build.sh --pack` to create the Nuget packages for the extensions

2. Ensure that there aren't any kernels running for the extension in question. It's generally best to close all notebooks opened in VS Code as they might be locking some of these files.

3. Run the `Polyglot Notebook: Create a new blank notebook` command in VS Code. Select `.dib` or `.ipynb` as the extension and any language as default.

4. Save the notebook anywhere you like

5. Run the `Polyglot Notebook: Restart the current Notebook's kernel` command

6. In the first code cell, paste this code
   - In the FolderName, give the path to the nuget cache. This should be `%userprofile%\.nuget\packages` on Windows and `~/.nuget/packages` on Mac/Linux
   - Also replace `EXTENSIONNAME` with the name of the extension (e.g. `microsoft.dotnet.interactive.sqlserver`)
   - On the `#i` line, fill in the path to the `dotnet-interactive` repo root
   - On the `#r` line, use the same `EXTENSIONNAME` above, and then look in the `artifacts\packages\Debug\Shipping` folder for the package you're using and get the version number from the name. e.g. a package named `Microsoft.DotNet.Interactive.SqlServer.1.0.0-dev.nupkg` would result in this line `#r "nuget: Microsoft.DotNet.Interactive.SqlServer, 1.0.0-dev"`

    ```powershell
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
   - If you get an error about access being denied, ensure that all other notebooks are closed and then restart the kernel again as in step 5.

8. Now, use the kernel as you normally would. You should see your local changes being used by the extension.


### Set up full suite of tests to run

Some tests require additional setup or will be skipped. `JupyterKernel` tests for example are set up to have the same test run against a Jupyter server, directly against the Jupyter kernel over ZMQ, and with a simulation of the messages. Jupyter server and Jupyter kernel tests require the following setup or will be skipped. The simulation tests can be run without additional steps. 

### Run tests with a local Jupyter Server

1. Install [Jupyter server](https://docs.jupyter.org/en/latest/install.html) or [Anaconda](https://www.anaconda.com/products/distribution)

2. [Install R kernel](https://docs.anaconda.com/anaconda/user-guide/tasks/using-r-language/) for R tests by calling the following in Anaconda Prompt (Windows) or the terminal (Mac/Linux)
```
conda install -c r r-irkernel
```
3. Start the server locally as mentioned [here](https://docs.jupyter.org/en/latest/running.html). You can use any random string or guid for your_token value.
```
jupyter notebook --no-browser --NotebookApp.token=<your_token> --port=8888
```
4. Set an environment variable `TEST_DOTNET_JUPYTER_HTTP_CONN` pointing to the server and the token you are using for the Jupyter server as 
```
--url http://localhost:8888 --token <your_token>
```
5. The tests will now use the environment variable to connect to your server. 

### Run tests with a Jupyter Kernel over ZMQ

1. Install [Anaconda](https://www.anaconda.com/products/distribution)

2. [Install R kernel](https://docs.anaconda.com/anaconda/user-guide/tasks/using-r-language/) for R tests by calling the following in Anaconda Prompt (Windows) or the terminal (Mac/Linux) 

    ```console
    conda install -c r r-irkernel
    ```

3. Set an environment variable `TEST_DOTNET_JUPYTER_ZMQ_CONN` pointing to conda installation and environment that has jupyter installed
```
--conda-env base
```

4. The tests will now use the environment variable to connect to your server. 

### Run tests directly against the language handler scripts

These tests can be run directly against the language handler scripts. This is useful for when making changes on the scripts sent to the jupyter kernel without needing a full integration.

1. Python tests can be run directly in the Anaconda Prompt with IPython by calling `src\Microsoft.DotNet.Interactive.Jupyter.Tests\LanguageHandlerTests\run_python_tests.bat`

2. R tests can be run directly in the Anaconda Prompt with RScript by calling `src\Microsoft.DotNet.Interactive.Jupyter.Tests\LanguageHandlerTests\run_r_tests.bat`

3. Both Python and R tests can be run together in the Anaconda Prompt by calling `src\Microsoft.DotNet.Interactive.Jupyter.Tests\LanguageHandlerTests\run_tests.bat`

