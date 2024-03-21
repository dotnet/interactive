# Updating to new version of stable

1. Run the following with the upcoming stable version of VS Code.  E.g., if preparing for version `1.83`, run the following:

```console
.\do-version-upgrade.ps1 -version 1.83
```

2. For each directory:
   - `<root>/src/polyglot-notebooks-vscode`
   - `<root>/src/polyglot-notebooks-vscode-insiders`
     - `npm i`
     - `npm run compile`
     - `npm run test`
3. `git add .`
4. `git commit`

## Validating

1. Use VSCode - Insiders to test the `stable` version of the extension.  Set the `dotnet-interactive.kernelTransportArgs` and `dotnet-interactive.notebookParserArgs` properties to use the locally-built tool.  Go through all scenarios in the `NotebookTestScript.dib` file at the root of the repo.

2. Use VSCode - Insiders to test the `insiders` version of the extension, exactly as above **EXCEPT** you'll have to manually drop the `engines.vscode` value in `package.json` since that version of Insiders doesn't exist yet.

# Locking stable to .NET Interactive version

After a `stable` release it is important to lock the vscode extension to the version of `.NET Interactive` so that later fixes can be release as vscode extension only without the need to publish a new set of nuget packages.

1. Run the following command to update `the package.json` for both `stable` and `insiders` and locks the vscode engine using the parameter `-version 1.87`

```console
\.update-versions.ps1 -updateAll -version 1.87
```