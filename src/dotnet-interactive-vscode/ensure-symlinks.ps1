function EnsureSymlink([string] $sourceLocation, [string] $destinationLocation) {
    if (Test-Path $sourceLocation) {
        Remove-Item $sourceLocation
    }

    New-Item -Path $sourceLocation -ItemType SymbolicLink -Value $destinationLocation
}

EnsureSymlink -sourceLocation "$PSScriptRoot\stable\src\common" -destinationLocation "$PSScriptRoot\common"
EnsureSymlink -sourceLocation "$PSScriptRoot\stable\.vscode" -destinationLocation "$PSScriptRoot\.vscode"

EnsureSymlink -sourceLocation "$PSScriptRoot\insiders\src\common" -destinationLocation "$PSScriptRoot\common"
EnsureSymlink -sourceLocation "$PSScriptRoot\insiders\.vscode" -destinationLocation "$PSScriptRoot\.vscode"
