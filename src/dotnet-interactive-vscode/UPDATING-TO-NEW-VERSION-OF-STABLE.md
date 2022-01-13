Updating to new version of stable
=================================

1. Copy `insiders\package.json` to `stable\`
2. Copy `insiders\src\*` to `stable\src\*` **EXCEPT** for the `common` symlinked directory.
3. Increment verion number in `vscodeStableVersion.txt`
4. `.\update-api.ps1`
5. `.\update-versions.ps1 -updateAll`
6. `git add .`, `git commit`

Validating
==========

1. Use VSCode - Insiders to test the `stable` version of the extension.  Set the `dotnet-interactive.kernelTransportArgs`
and `dotnet-interactive.notebookParserArgs` properties to use the locally-built tool.  Go through all scenarios in the
`NotebookTestScript.dib` file at the root of the repo.
2. Use VSCode - Insiders to test the `insiders` version of the extension, exactly as above **EXCEPT** you'll have to
manually drop the `engines.vscode` value in `package.json` since that version of Insiders doesn't exist yet.
