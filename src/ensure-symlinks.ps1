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

EnsureSymlink -sourceDirectory "$PSScriptRoot\polyglot-notebooks-browser\src" -linkName "polyglot-notebooks" -destinationLocation "..\..\polyglot-notebooks\src"

EnsureSymlink -sourceDirectory "$PSScriptRoot\polyglot-notebooks-vscode-common\src" -linkName "polyglot-notebooks" -destinationLocation "..\..\polyglot-notebooks\src"

EnsureSymlink -sourceDirectory "$PSScriptRoot\polyglot-notebooks-vscode\src" -linkName "ui" -destinationLocation "..\..\polyglot-notebooks-ui-components\src\contracts"
EnsureSymlink -sourceDirectory "$PSScriptRoot\polyglot-notebooks-vscode\src" -linkName "vscode-common" -destinationLocation "..\..\polyglot-notebooks-vscode-common\src"
EnsureSymlink -sourceDirectory "$PSScriptRoot\polyglot-notebooks-vscode\tests" -linkName "vscode-common-tests" -destinationLocation "..\..\polyglot-notebooks-vscode-common\tests"

EnsureSymlink -sourceDirectory "$PSScriptRoot\polyglot-notebooks-vscode-insiders\src" -linkName "ui" -destinationLocation "..\..\polyglot-notebooks-ui-components\src\contracts"
EnsureSymlink -sourceDirectory "$PSScriptRoot\polyglot-notebooks-vscode-insiders\src" -linkName "vscode-common" -destinationLocation "..\..\polyglot-notebooks-vscode-common\src"
EnsureSymlink -sourceDirectory "$PSScriptRoot\polyglot-notebooks-vscode-insiders\tests" -linkName "vscode-common-tests" -destinationLocation "..\..\polyglot-notebooks-vscode-common\tests"
