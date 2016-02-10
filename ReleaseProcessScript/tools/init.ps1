param($installPath, $toolsPath, $package)
Write-Output "Type 'get-help ReleaseProcessScript' to see all available ReleaseProcessScript commands"

Import-Module (Join-Path $toolsPath ReleaseProcessScript.psm1) -DisableNameChecking -Force