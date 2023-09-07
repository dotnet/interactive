Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

$thisDir = Split-Path -Parent $PSCommandPath
$toolLocation = ""
$toolVersion = ""

dotnet run --project (Join-Path -Path $thisDir ".." "interface-generator") --out-file (Join-Path $thisDir ".." "polyglot-notebooks" "src" "contracts.ts")

if (Test-Path 'env:DisableArcade') {
     dotnet pack (Join-Path $thisDir "dotnet-interactive.csproj") /p:Version=1.0.0
    $script:toolLocation = Join-Path $thisDir "bin" "debug"
    $script:toolVersion = "1.0.0"
} else {
    if ($IsLinux -or $IsMacOS) {
        & "$thisDir/../../build.sh" --pack
    } else {
        & "$thisDir\..\..\build.cmd" -pack
    }

    $script:toolLocation = Join-Path $thisDir ".." ".." "artifacts" "packages" "Debug" "Shipping"
    $script:toolVersion = "1.0.0-dev"
}

if (Get-Command dotnet-interactive -ErrorAction SilentlyContinue) {
    dotnet tool uninstall -g Microsoft.dotnet-interactive 
}
dotnet tool install -g --add-source "$toolLocation" --version $toolVersion Microsoft.dotnet-interactive
