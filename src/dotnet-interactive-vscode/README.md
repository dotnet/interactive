👉👉👉 This extension is still **under development**.

👉👉👉 Latest **VS Code Insiders** is required and at times this extension might be broken.

---

# .NET Interactive Notebooks

This extension adds support for using the `dotnet-interactive` global tool from within VS Code in a notebook-like environment.

## Getting Started

1. Install the [.NET SDK 3.1](https://dotnet.microsoft.com/download/visual-studio-sdks).
1. Install the latest [.NET Interactive Global Tool](https://www.nuget.org/packages/Microsoft.dotnet-interactive/).
1. Install the latest [VS Code Insiders](https://code.visualstudio.com/insiders/).
1. Open this directory with `code-insiders` and F5
1. Open a file with the `.dotnet-interactive` extension.

## Development

Same steps as in [Getting Started](#Getting-Started).  Periodically the notebook APIs will change; the latest API can
be obtained by running the `.\update-proposed-api.ps1` script.

## Deployment

To create an installable `.vsix` and use it in the latest VS Code Insiders build, run the following commands:

``` bash
# package
npm run package

# deploy
code-insiders --install-extension ./dotnet-interactive-vscode-42.42.42.vsix

#launch
code-insiders --enable-proposed-api ms-dotnettools.dotnet-interactive-vscode
```
