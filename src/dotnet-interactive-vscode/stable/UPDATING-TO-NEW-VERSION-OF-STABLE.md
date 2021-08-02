Updating to new version of stable
=================================

1. `.\update-api.ps1`
2. Copy `insiders\package.json` to `stable\`
3. Copy `insiders\src\*` to `stable\src\*` **EXCEPT** for the `common` symlinked directory.
4. Update verion number in `vscodeStableVersion.txt`
5. `git add .`, `git commit`
6. `.\update-api.ps1`
7. `.\update-versions.ps1 -updateAll`
8. Verify there are no surprises in the changes between steps 6 and this.

At this point `stable\src\vscode.d.ts` (and `vscode.proposed.d.ts`) will exactly equal the files in `insiders\src`.

Validating
==========

1. Use VSCode - Insiders to test the `stable` version of the code.  Go through all scenarios in the `NotebookTestScript.dib` file at the root of the repo.
2. Use VSCode - Insiders to test the `insiders` version of the code, exactly as above **EXCEPT** you'll have to manually drop the `engines.vscode` value in `package.json`
since that version of Insiders doesn't exist yet.
