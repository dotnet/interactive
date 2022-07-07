Updating to new version of stable
=================================

1. Copy `<root>/src/dotnet-interactive-vscode-insiders/package.json` to `<root>/src/dotnet-interactive-vscode/`
2. Copy `<root>/src/dotnet-interactive-vscode-insiders/src/*` to `<root>/src/dotnet-interactive-vscode/src/` **EXCEPT** for the `vscode-common` symlinked directory.
3. Increment verion number in `vscodeStableVersion.txt` to match the upcoming stable release of VS Code.
4. `.\update-api.ps1`
5. `.\update-versions.ps1 -updateAll`
6. For each directory:
   - `<root>/src/dotnet-interactive-vscode`
   - `<root>/src/dotnet-interactive-vscode-insiders`
   - `<root>/src/dotnet-interactive-vscode-ads`
     - `npm i`
     - `npm run compile`
     - `npm run test`
7. `git add .`, `git commit`

Validating
==========

1. Use VSCode - Insiders to test the `stable` version of the extension.  Set the `dotnet-interactive.kernelTransportArgs`
and `dotnet-interactive.notebookParserArgs` properties to use the locally-built tool.  Go through all scenarios in the
`NotebookTestScript.dib` file at the root of the repo.
2. Use VSCode - Insiders to test the `insiders` version of the extension, exactly as above **EXCEPT** you'll have to
manually drop the `engines.vscode` value in `package.json` since that version of Insiders doesn't exist yet.
3. Use VSCode - Stable to test the Azure Data Studio version of the extension in `<root>/src/dotnet-interactive-vscode-ads`.  You'll need the latest Azure Data Studio - Insiders installed.
