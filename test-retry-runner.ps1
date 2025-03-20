[CmdletBinding(PositionalBinding = $false)]
param (
    [int]$retryCount = 5,
    [string]$buildConfig = "Debug"
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

if ($IsWindows) {
    $projectsToSkip = @(
        )
}
else
{
    $projectsToSkip = @(
        "Microsoft.DotNet.Interactive.NetFramework.Tests",
        "Microsoft.DotNet.Interactive.NamedPipeConnector.Tests",
        "Microsoft.DotNet.Interactive.VisualStudio.Tests"
        )
}

function ExecuteTestDirectory([string]$testDirectory, [string]$extraArgs = "") {
    $testCommand = "dotnet test $testDirectory/ $extraArgs -l trx --no-restore --no-build -bl:$repoRoot/artifacts/TestResults/$buildConfig/test.binlog --blame-hang-timeout 10m --blame-hang-dump-type full --blame-crash -c $buildConfig --results-directory $repoRoot/artifacts/TestResults/$buildConfig"
    Write-Host "Executing $testCommand"
    Invoke-Expression $testCommand
}

try {
    $repoRoot = Resolve-Path $PSScriptRoot

    $testAssemblyDirectories = Get-ChildItem -Path "$repoRoot/src" -Directory -Filter *.Tests -Recurse

    foreach ($testAssemblyDirectory in $testAssemblyDirectories) {
        $projectName = $testAssemblyDirectory.Name
        if($projectsToSkip.contains($projectName)){
            Write-Host "Skipping test project $projectName"
            continue
        }
        for ($i = 1; $i -le $retryCount; $i++) {
            Write-Host "Testing project $projectName, attempt $i"
            ExecuteTestDirectory -testDirectory $testAssemblyDirectory
            if ($LASTEXITCODE -eq 0) {
                break
            }
        }
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}