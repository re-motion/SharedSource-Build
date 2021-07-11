$Location = $PSScriptRoot

. $Location"\credential_functions.ps1"
. $Location"\load_dll.ps1"

$JiraUrlPostFix = "rest/api/2/"

function Add-JiraUrlPostfix-To-Config-Url ($ConfigUrl)
{
  if ($ConfigUrl.EndsWith("/") )
  {
    $ReturnUrl = "$($ConfigUrl)$($JiraUrlPostFix)"
  }
  else
  {
    $ReturnUrl = "$($ConfigUrl)/$($JiraUrlPostFix)"    
  }

  return $ReturnUrl
}

function Add-Authentication ($JiraObject)
{
  if (-not (Config-Use-NTLM) )
  {
    $Credential = Get-Credential
    $JiraObject.JiraUsername = $Credential.UserName
    $JiraObject.JiraPassword = $Credential.Password
  }
}

function Check-For-Empty-Config ($ConfigFile)
{
  if ([string]::IsNullOrWhiteSpace($ConfigFile.settings.jira.jiraUrl) )
  {
    throw "Please enter the Jira Url into the config file."
  }

  if ([string]::IsNullOrWhiteSpace($ConfigFile.settings.jira.jiraProjectKey) )
  {
    throw "Please enter the Jira Project Key into the config file."
  }
}


function Jira-Create-Version ($Version)
{
  Confirm-Class-Loaded
  $JiraCreateVersion = New-Object Remotion.ReleaseProcessScript.Jira.JiraCreateNewVersionWithVersionNumber
  $ConfigFile = Get-Config-File

  Check-For-Empty-Config $ConfigFile

  $JiraCreateVersion.JiraUrl = Add-JiraUrlPostfix-To-Config-Url $ConfigFile.settings.jira.jiraUrl
  $JiraCreateVersion.JiraProjectKey = $ConfigFile.settings.jira.jiraProjectKey
  $JiraCreateVersion.VersionNumber = $Version

  Add-Authentication $JiraCreateVersion

  try 
  {
    $JiraCreateVersion.Execute()
  } 
  catch 
  {
    [System.InvalidOperationException]
    # Rethrow C# exception because the PS script does not terminate otherwise
    throw $_.Exception.Message
  }

  return $JiraCreateVersion.CreatedVersionID
}

function Jira-Get-Current-Version ()
{
  Confirm-Class-Loaded
  $JiraGetVersion = New-Object Remotion.ReleaseProcessScript.Jira.JiraGetEarliestUnreleasedVersion 

  $ConfigFile = Get-Config-File
  Check-For-Empty-Config $ConfigFile
    
  $JiraGetVersion.JiraUrl = Add-JiraUrlPostfix-To-Config-Url($ConfigFile.settings.jira.jiraUrl)
  $JiraGetVersion.JiraProject = $ConfigFile.settings.jira.jiraProjectKey
  $JiraGetVersion.VersionPattern = "(?s).*"

  Add-Authentication $JiraGetVersion

  try
  {
    $JiraGetVersion.Execute()
  }
  catch
  {
    # Rethrow C# exception because the PS script does not terminate otherwise
    throw $_.Exception.Message
  }

  $Version = New-Object -TypeName PSObject
  $Version |Add-Member -MemberType NoteProperty -Name VersionName -Value $JiraGetVersion.VersionName
  $Version |Add-Member -MemberType NoteProperty -Name VersionID -Value $JiraGetVersion.VersionID

  return $Version
}

function Jira-Release-Version ($CurrentVersionID, $NextVersionID, $SquashUnreleased)
{
  Confirm-Class-Loaded
  $ConfigFile = Get-Config-File
  Check-For-Empty-Config $ConfigFile

  Confirm-Class-Loaded

  if ($SquashUnreleased)
  {
    $JiraReleaseVersion = New-Object Remotion.ReleaseProcessScript.Jira.JiraReleaseVersionAndSquashUnreleased
    $JiraReleaseVersion.ProjectKey = $ConfigFile.settings.jira.jiraProjectKey

  }
  else
  {
    $JiraReleaseVersion = New-Object Remotion.ReleaseProcessScript.Jira.JiraReleaseVersion
  }
    
    
  $JiraReleaseVersion.JiraUrl = Add-JiraUrlPostfix-To-Config-Url $ConfigFile.settings.jira.jiraUrl
  $JiraReleaseVersion.VersionID = $CurrentVersionID
  $JiraReleaseVersion.NextVersionID = $NextVersionID
    
  Add-Authentication $JiraReleaseVersion

  try
  {
    $JiraReleaseVersion.Execute()
  }
  catch
  {
    # Rethrow C# exception because the PS script does not terminate otherwise
    throw $_.Exception.Message
  }
}

function Jira-Check-Credentials ($UserName, $Password)
{
  Confirm-Class-Loaded
  $JiraCheckAuthentication = New-Object Remotion.ReleaseProcessScript.Jira.JiraCheckAuthentication

  $ConfigFile = Get-Config-File
  Check-For-Empty-Config $ConfigFile
  $JiraUrl = Add-JiraUrlPostfix-To-Config-Url $ConfigFile.settings.jira.jiraUrl

  $JiraCheckAuthentication.JiraUrl = $JiraUrl
  $JiraCheckAuthentication.JiraUsername = $UserName
  $JiraCheckAuthentication.JiraPassword = $Password
  $JiraCheckAuthentication.JiraProject = $ConfigFile.settings.jira.jiraProjectKey
    
  try
  {
    $JiraCheckAuthentication.Execute()    
  }
  catch
  {
    # Rethrow C# exception because the PS script does not terminate otherwise
    $ErrorMessage = $_.Exception.Message
    throw "Jira Check Authentication has failed. Maybe wrong credentials? \nAlso be advised that the ProjectKey is case sensitive '$($ConfigFile.settings.jira.jiraProjectKey)' " +
	    "\nJira Url: '$($JiraUrl)'. \nException Message: $($_.Exception.Message)"
  }
}

function Confirm-Class-Loaded ()
{
  try
  {
    #We just use a random Classname to check if it is loaded
    if (-not ([Remotion.ReleaseProcessScript.Jira]'JiraCheckAuthentication').Type)
    {
      Load-Dependency-Dll
    }
  }
  catch
  {
    Load-Dependency-Dll
  }
}