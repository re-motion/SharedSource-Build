[xml]$global:ConfigFile = $NULL

function Load-Config-File ()
{
  $ConfigFilePath = Get-Config-File-Path

  if (-not (Test-Path $ConfigFilePath))
  {
    throw "Config file not found at '$($ConfigFilePath)'."
  }
    
  $global:ConfigFile = Get-Content $ConfigFilePath
}

function Get-Config-File ()
{
  return $global:ConfigFile
}

function Get-Config-File-Path ()
{
    $TopLevelPath = git rev-parse --show-toplevel 2>&1
    $MarkerFile = Get-Marker-File

    return (Join-path $TopLevelPath $MarkerFile.configFile.path)
}

function Get-Marker-File ()
{
    $TopLevelPath = git rev-parse --show-toplevel 2>&1
    $MarkerName = ".BuildProject"

    $MarkerPath = Join-Path $TopLevelPath $MarkerName

    if (-not (Test-Path $MarkerPath) )
    {
      throw ".BuildProject not found. We expect it to be at the .git Repository Top Level '$TopLevelPath'. If your Solution File is not in this Directory either, expect more issues to pop up (see Feature Request RMSRCBUILD-66)"
    }

    return [xml]$MarkerFile = Get-Content $MarkerPath
}

function Config-Use-NTLM
{
    $ConfigFile = Get-Config-File
    $UseNTLM = $ConfigFile.settings.jira.useNTLM

    if ( ($UseNTLM.ToUpper() -eq "YES") -or ($UseNTLM.ToUpper() -eq "Y") -or ($UseNTLM.ToUpper() -eq "T") -or ($UseNTLM.ToUpper() -eq "TRUE") )
    {
      return $TRUE
    }
    else
    {
      return $FALSE
    }
    
    return $FALSE 
}