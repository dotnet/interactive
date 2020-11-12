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

try {
    if (isCi -eq $true) {

        $sqlVersion="3.0.0-release.52"
        $downloads=(Join-Path $PSScriptRoot "..\artifacts\downloads")
        . (Join-Path $PSScriptRoot "DownLoadSqlToolsService.ps1") Release -out $downloads -version "v$sqlVersion"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        $outputPath=(Join-Path $PSScriptRoot "..\artifacts\packages\Release\Shipping")
        md $outputPath

        $projRoot=(Join-Path $PSScriptRoot "..\src\Microsoft.SqlToolsService")

        dotnet pack "$projRoot\runtime.osx-x64.native.Microsoft.SqlToolsService\runtime.osx-x64.native.Microsoft.SqlToolsService.csproj"         /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        dotnet pack "$projRoot\runtime.rhel-x64.native.Microsoft.SqlToolsService\runtime.rhel-x64.native.Microsoft.SqlToolsService.csproj"       /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        dotnet pack "$projRoot\runtime.win-x64.native.Microsoft.SqlToolsService\runtime.win-x64.native.Microsoft.SqlToolsService.csproj"         /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        dotnet pack "$projRoot\runtime.win-x86.native.Microsoft.SqlToolsService\runtime.win-x86.native.Microsoft.SqlToolsService.csproj"         /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        dotnet pack "$projRoot\runtime.win10-arm.native.Microsoft.SqlToolsService\runtime.win10-arm.native.Microsoft.SqlToolsService.csproj"     /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        dotnet pack "$projRoot\runtime.win10-arm64.native.Microsoft.SqlToolsService\runtime.win10-arm64.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        dotnet pack "$projRoot\Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    # invoke regular build/test script
    . (Join-Path $PSScriptRoot "common\build.ps1") -projects "$PSScriptRoot\..\dotnet-interactive.sln" @args
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}