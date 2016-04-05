param($installPath, $toolsPath, $package)

if(-not $global:AfterFirstInit)
{
  Write-Host "Type 'get-help ReleaseProcessScript' to see all available ReleaseProcessScript commands."
  $global:AfterFirstInit = $TRUE
}

Import-Module (Join-Path $toolsPath ReleaseProcessScript.psm1) -DisableNameChecking -Force