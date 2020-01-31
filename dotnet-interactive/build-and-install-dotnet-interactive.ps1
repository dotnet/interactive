Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

$thisDir = Split-Path -Parent $PSCommandPath
$toolLocation = ""
$toolVersion = ""
if (Test-Path 'env:DisableArcade') {
    dotnet pack "$thisDir\dotnet-interactive.csproj" /p:Version=0.0.0
    $script:toolLocation = "$thisDir\bin\debug"
    $script:toolVersion = "0.0.0"
} else {
    if ($IsLinux -or $IsMacOS) {
        & "$thisDir\..\build.sh" --pack
    } else {
        & "$thisDir\..\build.cmd" -pack
    }

    $script:toolLocation = "$thisDir\..\artifacts\packages\Debug\Shipping"
    $script:toolVersion = "1.0.44142.42"
}

if (Get-Command dotnet-interactive -ErrorAction SilentlyContinue) {
    dotnet tool uninstall -g Microsoft.dotnet-interactive
}
dotnet tool install -g --add-source "$toolLocation" --version $toolVersion Microsoft.dotnet-interactive
