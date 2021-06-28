function EnsureSymlink([string]$sourceDirectory, [string] $linkName, [string] $destinationLocation) {
    Push-Location $sourceDirectory

    try {
        if (Test-Path $linkName) {
            Remove-Item $linkName
        }

        cmd /c mklink /D $linkName $destinationLocation
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
    finally {
        Pop-Location
    }
}


EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-vscode\stable\src" -linkName "common" -destinationLocation "..\..\common"
EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-vscode\stable" -linkName ".vscode" -destinationLocation "..\.vscode"

EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-vscode\insiders\src" -linkName "common" -destinationLocation "..\..\common"
EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-vscode\insiders" -linkName ".vscode" -destinationLocation "..\.vscode"

EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-npm\src" -linkName "common" -destinationLocation "..\..\dotnet-interactive-vscode\common"
EnsureSymlink -sourceDirectory "$PSScriptRoot\Microsoft.DotNet.Interactive.Js\src" -linkName "common" -destinationLocation "..\..\dotnet-interactive-vscode\common"
