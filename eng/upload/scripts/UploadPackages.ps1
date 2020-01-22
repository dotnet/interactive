[CmdletBinding(PositionalBinding=$false)]
param (
    [string]$apiKey,
    [string]$feedUrl,
    [string]$packagesDir
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $packagePatterns = @(
        "Microsoft.dotnet-interactive.*.nupkg",
        "Microsoft.DotNet.Interactive.*.nupkg", # note that this also matches Microsoft.DotNet.Interactive.Formatting
        "dotnet-try.*.nupkg",
        "Microsoft.DotNet.Try.ProjectTemplate.Tutorial.*.nupkg",
        "MLS.Blazor.*.nupkg",
        "MLS.WasmCodeRunner.*.nupkg",
        "WorkspaceServer.*.nupkg"
    )

    $errors = 0
    foreach ($pattern in $packagePatterns) {
        foreach ($packageName in (Get-Item "$packagesDir\$pattern")) {
            $fullPath = $packageName.ToString()
            Write-Host "Uploading package $fullPath."
            $response = Invoke-WebRequest -Uri $feedUrl -Headers @{"X-NuGet-ApiKey"=$apiKey} -ContentType "multipart/form-data" -InFile "$fullPath" -Method Post -UseBasicParsing
            if ($response.StatusCode -ne 201) {
                Write-Host "Failed to upload package.  Status code: $response.StatusCode."
                $errors++
            }
            else {
                Write-Host "Done."
            }
        }
    }

    exit $errors

}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
