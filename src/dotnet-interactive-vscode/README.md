This extension is currently **_in preview_**. [Visual Studio Code Insiders](https://code.visualstudio.com/insiders/)  is required.
_The Visual Studio Code notebook support that this extension uses is also in preview and design is ongoing, so the extension might not work._
---

# .NET Interactive Notebooks

This extension adds support for using .NET Interactive in a Visual Studio Code notebook.

## Getting Started

1.  Install the latest [Visual Studio Code Insiders](https://code.visualstudio.com/insiders/).

2.  Install the latest [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) 

3.  Install the .NET Interactive Notebooks extension from the [marketplace](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode).

## Creating and editing notebooks

To open an existing Jupyter notebook (`.ipynb`), run the VS Code command `.NET Interactive: Open notebook` and select the file you would like to open.

You can create a notebook by creating a new file with the `.dib` extension and opening it. 

Currently, to create a new `.ipynb` notebook, you must first create a `.dib` and open it, then run the VS Code command `.NET Interactive: Save notebook as specific file format`.

**_A note on the `.dib` file format:_** _Improved support for reading and writing Jupyter notebooks (`.ipynb`) is coming soon. For more information, please see https://github.com/dotnet/interactive/issues/467_.
