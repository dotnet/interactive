[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$packageName,
    [string]$registryUrl,
    [string]$registryUser,
    [string]$registryEmail,
    [string]$currentBranch,
    [string]$publishingBranch,
    [string]$artifactDirectory,
    [string]$registryPublishToken
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $feedUrl = $registryUrl
    if ($feedUrl.EndsWith("registry/")) {
        $feedUrl = $feedUrl.Substring(0, $feedUrl.Length - "registry/".Length)
    }

    # create .npmrc with package feed
    $registryPublishTokenBase64 = [Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($registryPublishToken))
    $npmrcContents = "
; begin auth token
//$registryUrl`:username=$registryUser
//$registryUrl`:_password=$registryPublishTokenBase64
//$registryUrl`:email=$registryEmail
//$feedUrl`:username=$registryUser
//$feedUrl`:_password=$registryPublishTokenBase64
//$feedUrl`:email=$registryEmail
; end auth token"

    $npmrcContents | Out-File "$artifactDirectory/.npmrc"

    # publish to feed
    if (($currentBranch -Eq $publishingBranch) -Or ($currentBranch -Eq "refs/heads/$publishingBranch")) {
        $singlePackageName = Get-ChildItem "$artifactDirectory/$packageName" | Select-Object -First 1
        Write-Host "Publishing $singlePackageName to $registryUrl"
        npm publish "$singlePackageName" --access public
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
    else {
        Write-Host "Branch '$currentBranch' does not match publishing branch '$publishingBranch', skipping publish."
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
