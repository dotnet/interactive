Developer Instructions
=====================

## Preparing for local builds

1. Install [VS Code Insiders](https://code.visualstudio.com/insiders/).

2. Install LTS version of [nodejs](https://nodejs.org/en/download/).

## Testing with local changes to the VS Code extension

0. **Pre-build step**: run `npm i` in this directory.

1. Open this directory in VS Code Insiders: `code-insiders .`

2. Make appropriate changes to the `src/` directory.

3. F5.

## Testing with local changes to the .NET Interactive tool (e.g., the global tool)

1. Make appropriate changes and build the `<repo-root>/src/dotnet-interactive/dotnet-interactive.csproj` project.

2. In an instance of VS Code Insiders that has the extension installed (either via the marketplace or through the steps above), change the launch settings for the interactive tool:

   1. Open the settings via `File` -> `Preferences` -> `Settings`, or `Ctrl + ,`.

   2. Filter the list by typing `dotnet-interactive`

   3. Find the setting labelled: `Dotnet-interactive: Kernel transport args`.

   4. Click `Edit in settings.json`.

   5. In the file that opens, make the following changes, updating the path where appropriate:

      ``` diff
       "dotnet-interactive.kernelTransportArgs": [
         "{dotnet_path}",
      -  "tool",
      -  "run",
      -  "dotnet-interactive",
      -  "--",
      +  "E:/dotnet/interactive/artifacts/bin/dotnet-interactive/Debug/netcoreapp3.1/Microsoft.DotNet.Interactive.App.dll",
         "stdio",
         "--http-port-range",
         "1000-3000"
       ]
      ```

3. Save `settings.json` and close and re-open VS Code Insiders.

4. Any subsequently opened notebooks will use your local changes.

5. To revert back to the original settings, follow steps 1-3 then next to the text `Dotnet-interactive: Kernel transport args`, click the gear icon then `Reset Setting`.
