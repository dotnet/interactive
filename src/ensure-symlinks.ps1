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

EnsureSymlink -sourceDirectory "$PSScriptRoot\microsoft-dotnet-interactive-browser\src" -linkName "dotnet-interactive" -destinationLocation "..\..\microsoft-dotnet-interactive\src"

EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-vscode-common\src" -linkName "dotnet-interactive" -destinationLocation "..\..\microsoft-dotnet-interactive\src"

EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-vscode\src" -linkName "vscode-common" -destinationLocation "..\..\dotnet-interactive-vscode-common\src"
EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-vscode\tests" -linkName "vscode-common-tests" -destinationLocation "..\..\dotnet-interactive-vscode-common\tests"

EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-vscode-insiders\src" -linkName "vscode-common" -destinationLocation "..\..\dotnet-interactive-vscode-common\src"
EnsureSymlink -sourceDirectory "$PSScriptRoot\dotnet-interactive-vscode-insiders\tests" -linkName "vscode-common-tests" -destinationLocation "..\..\dotnet-interactive-vscode-common\tests"
