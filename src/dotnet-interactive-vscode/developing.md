# Contributing to the Visual Studio Code extension

As Visual Studio Code notebook support is still in preview, you will need to have [Visual Studio Code Insiders](https://code.visualstudio.com/insiders/) installed.

---

# .NET Interactive Notebooks

This extension adds support for using .NET Interactive in a Visual Studio Code notebook.

## Getting Started

1. Install the [.NET SDK 3.1](https://dotnet.microsoft.com/download/visual-studio-sdks).
2. Install the latest [VS Code Insiders](https://code.visualstudio.com/insiders/).
3. In your terminal, run `npm i` in the `src\dotnet-interactive-vscode` directory.
4. Open the `src\dotnet-interactive-vscode` directory with `code-insiders` and press `F5`.
5. Open or create a file with a `.dib` extension

    OR 

   Open a Jupyter notebook using the VS Code command *Convert Jupyter notebook (.ipynb) to .NET Interactive notebook*.


    ![image](https://user-images.githubusercontent.com/547415/84576252-147a8800-ad68-11ea-8315-07757291710f.png)


Periodically the notebook APIs will change; the latest API can be obtained by running the `.\update-proposed-api.ps1` script.

## Working with a dev build of `dotnet-interactive`

You can build the Visual Studio Code extension separately from the `dotnet-interactive` tool. For details on how to work on the `dotnet-interactive` tool, please read [here](../../CONTRIBUTING.md#developer-guide). 

## Deployment

To create an installable `.vsix` and use it in the latest Visual Studio Code Insiders build, run the following commands:

``` bash
# package
npm run package

# deploy
code-insiders --install-extension ./dotnet-interactive-vscode-42.42.42.vsix

#launch
code-insiders --enable-proposed-api ms-dotnettools.dotnet-interactive-vscode
```
