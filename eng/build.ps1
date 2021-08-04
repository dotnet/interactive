Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function TestUsingNPM([string] $testPath) {
    Write-Host "Installing packages"
    Start-Process -PassThru -WindowStyle Hidden -WorkingDirectory $testPath -Wait npm "i"
    Write-Host "Testing"
    $test = Start-Process -PassThru -WindowStyle Hidden -WorkingDirectory $testPath -Wait npm "run ciTest"
    Write-Host "Done with code $($test.ExitCode)"
    return $test.ExitCode
}

$arguments = $args
function isCi {
    $isCi = $arguments | Select-String -Pattern '-ci' -CaseSensitive -SimpleMatch
    return ($isCi -ne "")
}
$isCi = isCi

function buildConfiguration {
    $release = $arguments | Select-String -Pattern ('release', 'debug') -SimpleMatch -CaseSensitive
    if ([System.String]::IsNullOrWhitespace($release) -eq $true) {
        return "Debug"
    }
    else {
        return "$release"
    }
}
$buildConfiguration = buildConfiguration

try {
    # if (isCi -eq $true) {
    #     . (Join-Path $PSScriptRoot "..\buildSqlTools.cmd") $buildConfiguration
    #     if ($LASTEXITCODE -ne 0) {
    #         exit $LASTEXITCODE
    #     }
    # }

    $repoRoot = Resolve-Path "$PSScriptRoot\.."
    $symlinkDirectories = @(
        "$repoRoot\src\dotnet-interactive-vscode\stable\src\common",
        "$repoRoot\src\dotnet-interactive-vscode\stable\.vscode",
        "$repoRoot\src\dotnet-interactive-vscode\insiders\src\common",
        "$repoRoot\src\dotnet-interactive-vscode\insiders\.vscode",
        "$repoRoot\src\dotnet-interactive-npm\src\common",
        "$repoRoot\src\dotnet-interactive-npm\.vscode",
        "$repoRoot\src\Microsoft.DotNet.Interactive.Js\src\common",
        "$repoRoot\src\Microsoft.DotNet.Interactive.Js\.vscode"
    )

    foreach ($symlinkDir in $symlinkDirectories) {
        $candidate = Get-Item $symlinkDir -ErrorAction SilentlyContinue
        if (($null -eq $candidate) -Or (-Not($candidate.Attributes -match "ReparsePoint"))) {
            throw "The directory '$symlinkDir' was not a symlink.  Please run the script '$repoRoot\src\ensure-symlinks.ps1' **AS ADMIN**."
        }
    }

    # invoke regular build/test script
    . (Join-Path $PSScriptRoot "common\build.ps1") -projects "$PSScriptRoot\..\dotnet-interactive.sln" @args
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}