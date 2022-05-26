# Links for internal team members.

## Test Script

Open the VS Code extension test script in VS Code - Insiders

```
vscode-insiders://ms-dotnettools.dotnet-interactive-vscode/openNotebook?url=https%3A%2F%2Fraw.githubusercontent.com%2Fdotnet%2Finteractive%2Fmain%2FNotebookTestScript.dib
```

If the URL provided does not end in the notebook file's extension, you can specify the `notebookFormat` query parameter as an override with the supported values of 'dib' and 'ipynb'.

E.g.,

```
vscode-insiders://ms-dotnettools.dotnet-interactive-vscode/openNotebook?notebookFormat=ipynb&url=https://contoso.com/myNotebook
```

URL redirects are supported by this scenario and the extension and/or `notebookFormat` parameter will be pulled from the final resolved URL.

## PR Build Definition

The PR build definition can be found [here](https://dev.azure.com/dnceng/public/_build?definitionId=744&_a=summary) or by nagivating through an existing PR.

## Signed Build Definition

[Signed build definition](https://dev.azure.com/dnceng/internal/_build?definitionId=743&_a=summary)

## NuGet Package Upload

NuGet packages produced from every build of `main` are auto-published to the NuGet feed `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json`

## Publish VS Code Extension

The signed build produces three versions of the VS Code extension, 2 against Stable VS Code and 1 against Insiders.  Both versions against Stable append a `"0"` character to the extension version number and Insiders appends a `"1"` character.

### Stable with locked tool version

The Stable extension with the locked tool version can be published via [this](https://dev.azure.com/dnceng/internal/_release?_a=releases&view=mine&definitionId=86) release definition.  **This will also immediately publish the corresponding Insiders version of the extension.**

### Stable with latest tool

The Stable extension with the latest tool can be published via [this](https://dev.azure.com/dnceng/internal/_release?_a=releases&view=mine&definitionId=115) release definition.  **This will also immediately publish the corresponding Insiders version of the extension.**

### Insiders with latest tool

The Insiders extension with the latest tool can be published via [this](https://dev.azure.com/dnceng/internal/_release?_a=releases&view=mine&definitionId=103) release definition.

Once any of those release definitions are invoked, the new extension will appear in the VS Code marketplace approximately 10 minutes later, after the marketplace has run their own internal validation.

The publish/verification script is located in this repo at [`eng/publish/PublishVSCodeExtension.ps1`](eng/publish/PublishVSCodeExtension.ps1).

### Publish token for VS Code Marketplace

The variable group [`dotnet-interactive-api-keys`](https://dev.azure.com/dnceng/internal/_apps/hub/ms.vss-distributed-task.hub-library?itemType=VariableGroups&view=VariableGroupView&variableGroupId=107&path=dotnet-interactive-api-keys) contains the secret `vscode-marketplace-dotnet-tools-publish-token` which holds the PAT used to upload the `.vsix` to the VS Code Marketplace.  If this PAT needs to be regenerated:

1. Download the latest `vsm.mac.pat` package from `https://dev.azure.com/devdiv/OnlineServices/_artifacts/feed/vsmarketplace`
2. From a `pwsh` prompt run `dotnet tool install --global --add-source "$env:USERPROFILE\Downloads" vsm.mac.pat`
3. Run `vsmpat generate`.  You'll be prompted to login through a web browser and 8-10 seconds later the PAT will appear on the console.

### Rolling back to an older version of the VS Code Extension

To roll back to a previous build, you'll need to:

1. Find the build you'd like to roll back to.
2. Copy the commit's SHA.
3. Start a new signed build with that SHA.
4. Use that build to publish like normal.

This ensures that the version number of the extension is always increasing to pass the VS Code Marketplace verification.

## Internal Code Mirror

The public GitHub code is internally mirrored [here](https://dev.azure.com/dnceng/internal/_git/dotnet-interactive) to enable signed builds.  You'll likely never need to do anything with this.
