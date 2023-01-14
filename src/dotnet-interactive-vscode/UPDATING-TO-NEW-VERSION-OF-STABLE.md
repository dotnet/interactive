# Updating to new version of stable

1. Run the following with the upcoming stable version of VS Code.  E.g., if preparing for version `1.74`, run the following:

```console
.\do-version-upgrade.ps1 -version 1.74
```

2. For each directory:
   - `<root>/src/dotnet-interactive-vscode`
   - `<root>/src/dotnet-interactive-vscode-insiders`
     - `npm i`
     - `npm run compile`
     - `npm run test`
3. `git add .`
4. `git commit`

## Validating

1. Use VSCode - Insiders to test the `stable` version of the extension.  Set the `dotnet-interactive.kernelTransportArgs` and `dotnet-interactive.notebookParserArgs` properties to use the locally-built tool.  Go through all scenarios in the `NotebookTestScript.dib` file at the root of the repo.

2. Use VSCode - Insiders to test the `insiders` version of the extension, exactly as above **EXCEPT** you'll have to manually drop the `engines.vscode` value in `package.json` since that version of Insiders doesn't exist yet.
