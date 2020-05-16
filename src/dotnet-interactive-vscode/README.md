👉👉👉 This extension is still **under development**.

👉👉👉 Latest **VS Code Insiders** is required and at times this extension might be broken.

---

# .NET Interactive Notebooks

This extension adds support for using the `dotnet-interactive` global tool from within VS Code in a notebook-like environment.

## Getting Started

1. Install the [.NET SDK 3.1](https://dotnet.microsoft.com/download/visual-studio-sdks).
2. Install the latest [.NET Interactive Global Tool](https://www.nuget.org/packages/Microsoft.dotnet-interactive/).
3. Install the latest [VS Code Insiders](https://code.visualstudio.com/insiders/).
4. In your terminal, run `npm i` in this directory.
5. Open this directory with `code-insiders` and press `F5`.
6. Open a file with a `.dotnet-interactive` or `.ipynb` extension.

## Development

Same steps as in [Getting Started](#Getting-Started). Periodically the notebook APIs will change; the latest API can
be obtained by running the `.\update-proposed-api.ps1` script.
