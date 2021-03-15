function EnsureSymlink([string] $sourceLocation, [string] $destinationLocation) {
    if (Test-Path $sourceLocation) {
        Remove-Item $sourceLocation
    }

    New-Item -Path $sourceLocation -ItemType SymbolicLink -Value $destinationLocation
}

EnsureSymlink -sourceLocation "$PSScriptRoot\dotnet-interactive-vscode\stable\src\common" -destinationLocation "$PSScriptRoot\dotnet-interactive-vscode\common"
EnsureSymlink -sourceLocation "$PSScriptRoot\dotnet-interactive-vscode\stable\.vscode" -destinationLocation "$PSScriptRoot\dotnet-interactive-vscode\.vscode"

EnsureSymlink -sourceLocation "$PSScriptRoot\dotnet-interactive-vscode\insiders\src\common" -destinationLocation "$PSScriptRoot\dotnet-interactive-vscode\common"
EnsureSymlink -sourceLocation "$PSScriptRoot\dotnet-interactive-vscode\insiders\.vscode" -destinationLocation "$PSScriptRoot\dotnet-interactive-vscode\.vscode"

EnsureSymlink -sourceLocation "$PSScriptRoot\dotnet-interactive-npm\src\common" -destinationLocation "$PSScriptRoot\dotnet-interactive-vscode\common"
