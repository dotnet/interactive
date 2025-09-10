# Links for internal team members.

## Test Script

The test script notebook is used to verify hard-to-automate end-to-end scenarios.

To use it, open the [test script notebook](vscode-insiders://ms-dotnettools.dotnet-interactive-vscode/openNotebook?url=https%3A%2F%2Fraw.githubusercontent.com%2Fdotnet%2Finteractive%2Fmain%2FNotebookTestScript.dib) in VS Code - Insiders

If the URL provided does not end in the notebook file's extension, you can specify the `notebookFormat` query parameter as an override with the supported values of `.dib` and `.ipynb`m e.g.:

```
vscode-insiders://ms-dotnettools.dotnet-interactive-vscode/openNotebook?notebookFormat=ipynb&url=https://contoso.com/myNotebook
```

URL redirects are supported by this scenario and the extension and/or `notebookFormat` parameter will be pulled from the final resolved URL.

## PR Build Definition

The PR build definition can be found [here](https://dev.azure.com/dnceng-public/public/_build?definitionId=71) or by nagivating through an existing PR.

## Signed Build Definition

[Signed build definition](https://dev.azure.com/dnceng/internal/_build?definitionId=743&_a=summary)

## NuGet Package Upload

NuGet packages produced from every build of `main` are auto-published to the NuGet feed `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json`

## Consuming NPM Packages

The signed build and CI machines aren't allowed to access npmjs.org directly.  If you add or update a Node package and your PR fails with:

```
Building NPM in directory src\polyglot-notebooks-vscode-insiders
npm ERR! code E401
npm ERR! Unable to authenticate, your authentication token seems to be invalid.
npm ERR! To correct this please trying logging in again with:
npm ERR!     npm login

npm ERR! A complete log of this run can be found in:
npm ERR!     C:\Users\cloudtest\AppData\Local\npm-cache\_logs\2023-03-07T20_48_41_356Z-debug.log
```

...then you'll need to ensure the new packages are added to the internal NPM mirror.

To do this:

(this part only happens once)

1. Navigate to the internal NPM package feed: https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-public-npm
2. Click "Connect to Feed".
3. Select "npm".
4. Follow the "Project setup" instructions shown.

(this part may need to happen multiple times)

1. Clear your local NPM cache with `npm cache clean --force`.
2. Delete the `node_modules` directory in the directory where you added/updated the packages.
3. Re-run `npm install` in the directory where you added/updated the packages.

When you do this locally while authenticated, any packages _not_ on the internal mirror will be copied from npmjs.org.

## Publish VS Code Extension

### Publishing to insiders
You could use [this pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=1409) to publish the extension to insiders. Make sure that the build containing the changes has completed successfully [here](https://dev.azure.com/dnceng/internal/_build?definitionId=743&_a=summary) and matches the version you are trying to publish (by default this should automatically consider the latest successful signed build).

### Publishing to Stable
You could use [this pipeline] (https://dev.azure.com/dnceng/internal/_build?definitionId=1411) to publish to stable. Note that this also publishes the same version to insiders as well and publish our nuget packages to nuget.org. Make sure that the build containing the changes has completed successfully [here](https://dev.azure.com/dnceng/internal/_build?definitionId=743&_a=summary) and matches the version you are trying to publish (by default this should automatically consider the latest successful signed build).

The new extension will appear in the VS Code marketplace approximately 10 minutes later, after the marketplace has run their own internal validation.

The publish/verification script is located in this repo at [`eng/publish/PublishVSCodeExtension.ps1`](eng/publish/PublishVSCodeExtension.ps1).

### Setting up a branch for package publishing

1. (One time) Install the Arcade DARC tool by running `eng/common/darc-init.ps1` from a `pwsh` prompt.
2. (One time) Run the command `darc authenticate`.  A text file will be opened with instructions on how to populate access tokens.
3. (Side note) The help system in the `darc` tool is very good.  You can either run `darc --help` or `darc COMMAND --help` to get help on any command.
4. View the current channel publishing configuration by running `darc get-default-channels --source-repo dotnet/interactive`.  You will see several entries that look look like this:

   ```
   (2459) https://github.com/dotnet/interactive @ main -> .NET Core Tooling Dev
   ```

   This means that the `main` branch of `dotnet/interactive` is publishing packages to the `.NET Core Tooling Dev` channel.
5. Set the new branch to publish to the appropriate channel by running `darc add-default-channel --channel "THE CHANNEL NAME" --branch "feature/the-new-feature" --repo https://github.com/dotnet/interactive`.  The channel name you'll most likely use is `.NET Core Experimental` which corresponds to the `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json` NuGet feed.  To determine the name of any channel, see the below section.
6. Re-run step 3 to verify that the new branch is publishing to the correct channel.
7. Any new build from that branch will publish packages to the specified channel.
8. Be sure to update the appropriate `dotnet-interactive.interactiveToolSource` settings in the VS Code extension's `package.json` for both stable and insiders.

### Mapping a channel name to a NuGet package feed.

The mapping between a channel name and the corresponding NuGet package feed, e.g., `.NET Core Tooling Dev` => `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json` isn't straight forward.  I'll use the above values as an example.

1. View the [`PublishingConstants.cs`](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.Build.Tasks.Feed/src/model/PublishingConstants.cs) file in the `dotnet/arcade` repo on GitHub.
2. Search for the channel name, e.g., `.NET Core Tooling Dev`.  As of this writing the definition is [here](https://github.com/dotnet/arcade/blob/c0e25012be6fc00d0e5d1480b2ee4610f490e735/src/Microsoft.DotNet.Build.Tasks.Feed/src/model/PublishingConstants.cs#L694-L703).
3. Notice the `targetFeeds` parameter points to the `DotNetToolsFeeds` variable.
4. The `DotNetToolsFeeds` variable is defined [here](https://github.com/dotnet/arcade/blob/c0e25012be6fc00d0e5d1480b2ee4610f490e735/src/Microsoft.DotNet.Build.Tasks.Feed/src/model/PublishingConstants.cs#L175-L181) and it's entry for shipping packages lists another variable, `FeedDotNetTools`.
5. The [`FeedDotNetTools`](https://github.com/dotnet/arcade/blob/c0e25012be6fc00d0e5d1480b2ee4610f490e735/src/Microsoft.DotNet.Build.Tasks.Feed/src/model/PublishingConstants.cs#L85) variable lists the NuGet package feed.

### Rolling back to an older version of the VS Code Extension

To roll back to a previous build, you'll need to:

1. Find the build you'd like to roll back to.
2. Copy the commit's SHA.
3. Start a new signed build with that SHA.
4. Use that build to publish like normal.

This ensures that the version number of the extension is always increasing to pass the VS Code Marketplace verification.

## Internal Code Mirror

The public GitHub code is internally mirrored [here](https://dev.azure.com/dnceng/internal/_git/dotnet-interactive) to enable signed builds.  You'll likely never need to do anything with this.
