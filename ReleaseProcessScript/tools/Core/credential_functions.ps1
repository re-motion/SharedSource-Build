$Location = $PSScriptRoot

. $Location"\config_functions.ps1"
. $Location"\load_dll.ps1"

$jiraInvalidCredentialsExceptionString = "JIRA response: invalid credentials. Please try again."

function Save-Credential ($UserName, $Password)
{
  Delete-Credentials

  $PasswordCredential = Create-Password-Credential $UserName $Password

  [Windows.Security.Credentials.PasswordVault,Windows.Security.Credentials,ContentType=WindowsRuntime] > $NULL
  $PasswordVault = New-Object Windows.Security.Credentials.PasswordVault
  $PasswordVault.Add($PasswordCredential)
}

function Create-Password-Credential ($UserName, $Password)
{
  $ConfigFile = Get-Config-File
  $JiraCredentialResourceString = "$($ConfigFile.settings.jira.jiraUrl)"

  [Windows.Security.Credentials.PasswordVault,Windows.Security.Credentials,ContentType=WindowsRuntime] > $NULL
  $PasswordCredential = New-Object Windows.Security.Credentials.PasswordCredential

  $PasswordCredential.UserName = $UserName    
  $PasswordCredential.Password = $Password
  $PasswordCredential.Resource = $JiraCredentialResourceString

  return $PasswordCredential 
}

function Get-Credential ()
{
  [Windows.Security.Credentials.PasswordVault,Windows.Security.Credentials,ContentType=WindowsRuntime] > $NULL
  $PasswordVault = New-Object Windows.Security.Credentials.PasswordVault

  $ConfigFile = Get-Config-File
  $JiraCredentialResourceString = "$($ConfigFile.settings.jira.jiraUrl)"

  try
  {
    $Credential = $PasswordVault.FindAllByResource($JiraCredentialResourceString)
    $Credential.RetrievePassword()

    try
    {
      Check-Jira-Authentication $Credential.UserName $Credential.Password
    }
    catch
    {
      if ($_.Exception.Message -eq $jiraInvalidCredentialsExceptionString)
      {
        Write-Host "Invalid JIRA credentials saved"
        Ask-For-Credential
      }
      else
      {
        throw $_.Exception.Message         
      }
    }
  }
  catch [System.Management.Automation.MethodInvocationException]
  {
    return Ask-For-Credential
  }

  #Repackaging the properties is required to work around some weird Powershell issue where the credential object's values are emptied out on the callsite.
  return New-Object psobject -Property @{ UserName = $Credential.UserName; Password = $Credential.Password }
}

function Delete-Credentials ()
{
  $ConfigFile = Get-Config-File
  $JiraCredentialResourceString = "$($ConfigFile.settings.jira.jiraUrl)"

  [Windows.Security.Credentials.PasswordVault,Windows.Security.Credentials,ContentType=WindowsRuntime] > $NULL
  $PasswordVault = New-Object Windows.Security.Credentials.PasswordVault
  try
  {
    $Credentials = $PasswordVault.FindAllByResource($JiraCredentialResourceString)

  }
  catch [System.Management.Automation.MethodInvocationException]    
  {
    #In case we havent saved anything into PasswordVault before
    return
  }

  foreach($Credential in $Credentials)
  {
    $PasswordVault.Remove($Credential)
  }
}

function Ask-For-Credential ()
{
  Write-Host "Please enter your Jira Authentication Details"
  $UserName = Read-Host "Username"
  $PasswordSecure = Read-Host "Password" -AsSecureString

  $Password = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
  [Runtime.InteropServices.Marshal]::SecureStringToBSTR($PasswordSecure))

  Check-Jira-Authentication $UserName $Password

  $savePassword = Read-Host "Do you want to save the password? (Y/N)"

  if ($savePassword.ToUpper() -eq "Y")
  {
    Save-Credential $UserName $Password
    Write-Host "Saved password"
  }

  return Create-Password-Credential $UserName $Password
}

function Check-Jira-Authentication ($UserName, $Password)
{
  try 
  {
    Jira-Check-Credentials $UserName $Password
  }
  catch [Remotion.ReleaseProcessScript.Jira.ServiceFacadeImplementations.JiraException] 
  {
    $StatusCode = $_.Exception.HttpStatusCode

    if ($StatusCode -eq [System.Net.HttpStatusCode]::Forbidden -or $StatusCode -eq [System.Net.HttpStatusCode]::Unauthorized)
    {
      throw $jiraInvalidCredentialsExceptionString           
    }
    else
    {
      throw "Authentication failed. Returned statusCode '$($StatusCode)'. Exception details: $($_.Exception)"
    }
  }
}