[CmdletBinding(PositionalBinding = $false)]
param (
    [int]$retryCount = 5,
    [string]$buildConfig = "Debug"
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function ExecuteTestDirectory([string]$testDirectory, [string]$extraArgs = "") {
    $testCommand = "dotnet test $testDirectory/ $extraArgs -l trx --no-restore --no-build --blame-hang-timeout 15m --blame-hang-dump-type full --blame-crash -c $buildConfig --results-directory $repoRoot/artifacts/TestResults/$buildConfig"
    Write-Host "Executing $testCommand"
    Invoke-Expression $testCommand
}

try {
    $repoRoot = Resolve-Path $PSScriptRoot
    $flakyTestAssemblyDirectory = "Microsoft.DotNet.Interactive.Tests"
    $normalTestAssemblyDirectories = Get-ChildItem -Path "$repoRoot/src" -Directory -Filter *.Tests -Recurse | Where-Object { $_.Name -ne $flakyTestAssemblyDirectory }

    foreach ($testAssemblyDirectory in $normalTestAssemblyDirectories) {
        $assemblyName = $testAssemblyDirectory.Name
        for ($i = 1; $i -le $retryCount; $i++) {
            Write-Host "Testing assembly $assemblyName, attempt $i"
            ExecuteTestDirectory -testDirectory $testAssemblyDirectory
            if ($LASTEXITCODE -eq 0) {
                break
            }
        }
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    $testNamePattern = "    ([^(]+)" # skip 4 spaces then get everything that's not a left paren because test names start with 4 spaces and [Theory] tests have a parenthesized argument list
    $testNames = dotnet test "$repoRoot/src/$flakyTestAssemblyDirectory/" --no-restore --no-build --configuration $buildConfig --list-tests | Select-String -Pattern $testNamePattern | ForEach-Object { $_.Matches[0].Groups[1].Value }
    $testClasses = $testNames | ForEach-Object { $_.Substring(0, $_.LastIndexOf([char]".")) } # trim off the test name, just get the class
    $distinctTestClasses = $testClasses | Get-Unique

    foreach ($testClass in $distinctTestClasses) {
        for ($i = 1; $i -le $retryCount; $i++) {
            Write-Host "Testing class $testClass, attempt $i"
            ExecuteTestDirectory -testDirectory "$repoRoot/src/$flakyTestAssemblyDirectory" -extraArgs "--filter FullyQualifiedName~$testClass"
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
