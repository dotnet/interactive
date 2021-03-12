# Links for internal team members.

## PR Build Definition

The PR build definition can be found [here](https://dev.azure.com/dnceng/public/_build?definitionId=744&_a=summary) or by nagivating through an existing PR.

## Signed Build Definition

[Signed build definition](https://dev.azure.com/dnceng/internal/_build?definitionId=743&_a=summary)

## NuGet Package Upload

NuGet packages produced from every build of `main` are auto-published to the NuGet feed `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json`

## Publish VS Code Extension

The repo produces two versions of the VS Code extension on every build; one for Stable, the other for Insiders, and they both target the same backing tool.

The Stable version of the extension can be published from a specified build via [this](https://dev.azure.com/dnceng/internal/_release?_a=releases&view=mine&definitionId=86) release definition.  The extension version number for Stable will always be the version of the internal tool plus another `"0"` character, e.g., if the tool version is `1.0.123456`, then the Stable extension will be version `1.0.1234560`.  Due to how the VS Code marketplace handles extensions, this _also_ will publish the corresponding Insiders version.

The Insiders version of the extension can be published from a specified build via [this](https://dev.azure.com/dnceng/internal/_release?_a=releases&view=mine&definitionId=103) release definition.  The extension number for Insiders will always be the version of the internal tool plus another `"1"` character, e.g., if the tool version is `1.0.123456`, then the Insiders extension will be version `1.0.1234561`.

Once either of those release definitions are invoked, the new extension will appear in the VS Code marketplace approximately 10 minutes later, after the marketplace has run their own internal validation.

The publish/verification script is located in this repo at [`eng/publish/PublishVSCodeExtension.ps1`](eng/publish/PublishVSCodeExtension.ps1).

### Rolling back to an older version of the VS Code Extension

To roll back to a previous build, you'll need to:

1. Find the build you'd like to roll back to.
2. Copy the commit's SHA.
3. Start a new signed build with that SHA.
4. Use that build to publish like normal.

This ensures that the version number of the extension is always increasing to pass the VS Code Marketplace verification.

## Internal Code Mirror

The public GitHub code is internally mirrored [here](https://dev.azure.com/dnceng/internal/_git/dotnet-interactive) to enable signed builds.  You'll likely never need to do anything with this.
