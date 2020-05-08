# dotnet-interactive-vscode README

### Development

Open this directory in [VS Code Insiders](https://code.visualstudio.com/insiders/) then F5.  The extension is
triggered by opening an existing file with the `.dotnet-interactive` extension.

The `dotnet-interactive` global tool is assumed to be installed and on the path.

Some APIs used in this extension are only in VS Code Insiders builds, and the file `./src/vscode.proposed.d.ts` is
required to be present.  A fresh copy of that file can be obtained from https://github.com/microsoft/vscode/blob/master/src/vs/vscode.proposed.d.ts.
A convenience script is located at `./update-proposed-api.ps1` that will fetch a fresh copy.

### Deployment

To create an installable `.vsix` and use it in the latest VS Code Insiders build, run the following commands:

``` bash
# package
npm run package

# deploy
code-insiders --install-extension ./dotnet-interactive-vscode-42.42.42.vsix

#launch
code-insiders --enable-proposed-api ms-dotnettools.dotnet-interactive-vscode
```
