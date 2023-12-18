Manually testing the Polyglot Extension for VS Code
===================================================

1. Install the latest VS Code - Insiders from [here](https://code.visualstudio.com/insiders/).
2. Install the latest dotnet 8 SDK from [here](https://dotnet.microsoft.com/en-us/download).
3. From the internal signed build ([here](https://dev.azure.com/dnceng/internal/_build?definitionId=743&_a=summary)), find the latest passing build from `main`.
4. Download the artifact `vscode/insiders/dotnet-interactive-vscode-*.vsix`.
5. Launch VS Code - Insiders, open the extensions tab, then manually install the VSIX downloaded in the previous step.
   - When installing the VSIX you may see an error message similar to this:

     ```
     Unable to install extension 'ms-dotnettools.dotnet-interactive-vscode' as it is not compatible with VS Code '1.XX.0-insider'.
     ```

     The primary time this may happen is during the first week of a month when new versions of VS Code are released and we're out-of-sync with each other.  If this occurs, you should be able to rewind to step 4 and instead download and install the extension from the `.../stable/...` directory.
6. Save the latest [NotebookTestScript.dib](https://github.com/dotnet/interactive/blob/main/NotebookTestScript.dib) to disk and open in VS Code - Insiders.
7. Follow the prompts in the notebook.
