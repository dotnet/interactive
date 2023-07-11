# Developer Guide: Polyglot Notebooks extension for Visual Studio Code

The organization of this extension is a bit complicated.
===

This repo depends on symbolic links between directories.  By default Windows doesn't support this feature.  To work around this scenario, please run the PowerShell script `<root>/src/ensure-symlinks.ps1` as an administrator.  This usually only needs to be run once. If you run `git clean` you will need to repeat this step.

## Setup steps

0. Requirements:
    * nodejs v12.16.1.
    * npm v6.14.11.
    * Shell requirements:
      * git **with symlink support**

      **_or_**

      * Latest stable [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/) on the path 
      
        **_and_** 
      
      * [Developer Mode](https://docs.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development) enabled for Windows.

1. Open a terminal in either the `src/polyglot-notebooks-vscode/` or `src/polyglot-notebooks-insiders/` directory.

2. `npm install`

3. Open the appropriate VS Code.
   
   For VS Code stable run:

    ```console
    code .
    ```

    For VS Code Insiders run:

    ```console
    code-insiders .
    ```

4. Press F5.

## The complicated bits

The vast majority of the code is shared between the regular and insiders versions of the extension and it lives in the
`src/polyglot-notebooks-common/` directory.  To enable the `src/polyglot-notebooks-vscode/` and `src/polyglot-notebooks-vscode-insiders/` directories to build, however, symlinks were added to
properly pull the common files into the source tree under `<extension-root>/src/vscode-common`.

If you have git configured to handel symlinks then you're good to go.  If not, you'll need to run the script
`.\ensure-symlinks.ps1` whenever you switch from a branch that previously didn't have the symlinks.
