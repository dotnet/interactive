Updating to new version of stable
=================================

1. Copy `<root>/src/dotnet-interactive-vscode-insiders/help/*` to `<root>/src/dotnet-interactive-vscode/help/`
2. Copy `<root>/src/dotnet-interactive-vscode-insiders/src/*` to `<root>/src/dotnet-interactive-vscode/src/` **EXCEPT** for the `vscode-common` symlinked directory.
3. Increment verion number in `vscodeStableVersion.txt` to match the upcoming stable release of VS Code.
4. `.\copy-package-json.ps1`
5. `.\update-api.ps1`
6. `.\update-versions.ps1 -updateAll`
7. For each directory:
   - `<root>/src/dotnet-interactive-vscode`
   - `<root>/src/dotnet-interactive-vscode-insiders`
     - `npm i`
     - `npm run compile`
     - `npm run test`
8. `git add .`, `git commit`

Validating
==========

1. Use VSCode - Insiders to test the `stable` version of the extension.  Set the `dotnet-interactive.kernelTransportArgs`
and `dotnet-interactive.notebookParserArgs` properties to use the locally-built tool.  Go through all scenarios in the
`NotebookTestScript.dib` file at the root of the repo.
2. Use VSCode - Insiders to test the `insiders` version of the extension, exactly as above **EXCEPT** you'll have to
manually drop the `engines.vscode` value in `package.json` since that version of Insiders doesn't exist yet.
