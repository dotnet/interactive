# Contributing to the Visual Studio Code extension

As Visual Studio Code notebook support is still in preview, you will need to have [Visual Studio Code Insiders](https://code.visualstudio.com/insiders/) installed.

---

# .NET Interactive Notebooks

This extension adds support for using .NET Interactive in a Visual Studio Code notebook.

## Getting Started

1. Install the [.NET SDK 3.1](https://dotnet.microsoft.com/download/visual-studio-sdks).
2. Install the latest [.NET Interactive Global Tool](https://www.nuget.org/packages/Microsoft.dotnet-interactive/).
3. Install the latest [VS Code Insiders](https://code.visualstudio.com/insiders/).
4. In your terminal, run `npm i` in this directory.
5. Open this directory with `code-insiders` and press `F5`.
6. Open a file with a `.dotnet-interactive` or `.ipynb` extension.

Periodically the notebook APIs will change; the latest API can be obtained by running the `.\update-proposed-api.ps1` script.

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
