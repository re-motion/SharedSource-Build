function Parse-Version-From-BranchName ($Branchname)
{
  $SplitBranchname = $Branchname.Split(@("/v"), [System.StringSplitOptions]::None)

  if ($SplitBranchname.Length -ne 2)
  {
    throw "Current branch name is not in a valid format (e.g. release/v1.2.3)."
  }
    
  return $SplitBranchname[1]
}

function Create-And-Release-Jira-Versions ($CurrentVersion, $NextVersion, $SquashUnreleased)
{
  $CurrentVersionId = Jira-Create-Version $CurrentVersion
  $NextVersionId = Jira-Create-Version $NextVersion

  Write-Host "Releasing version '$($CurrentVersion) on JIRA."
  Write-Host "Moving open issues to '$($NextVersion)'."

  Jira-Release-Version $CurrentVersionId $NextVersionId $SquashUnreleased
}

function Find-Next-Patch ($LastVersion)
{
  $CurrentVersion = Get-Next-Patch $LastVersion
  
  while (Get-Tag-Exists v$($CurrentVersion))
  {
    $CurrentVersion = Get-Next-Patch $LastVersion
  }

  return $CurrentVersion
}

function Get-Develop-Current-Version ($StartReleasebranch)
{
  if ($StartReleasebranch)
  {
    $WithoutPrerelease = $TRUE
  }
  else
  {
    $WithoutPrerelease = $FALSE
  }

  if (-not (Get-Last-Version-Of-Branch-From-Tag-Exists) )
  {
    return Read-Current-Version
  }

  $MostRecentVersion = Get-Develop-Most-Recent-Version

  $PossibleVersions = Get-Possible-Next-Versions-Develop $MostRecentVersion $WithoutPrerelease
  Write-Host "Please choose Release Version:"
  $CurrentVersion = Read-Version-Choice $PossibleVersions

  return $CurrentVersion
}

function Get-Hotfix-Current-Version ($StartReleasePhase)
{
  if ($StartReleasePhase)
  {
    #Get version from name of current branch
    $CurrentBranchname = Get-Current-Branchname
    $CurrentVersion = Parse-Version-From-BranchName $CurrentBranchname
  }
  else
  {
    $MostRecentVersion = Get-Hotfix-Most-Recent-Version

    $PossibleVersions = Get-Possible-Versions-Hotfix $MostRecentVersion $true
    Write-Host "Please choose Release Version:"
    $CurrentVersion = Read-Version-Choice $PossibleVersions
  }

  return $CurrentVersion
}

function Get-Develop-Most-Recent-Version ()
{
  #Get last Tag from develop
  $DevelopVersion = Get-Last-Version-Of-Branch-From-Tag

  if (Get-Last-Version-Of-Branch-From-Tag-Exists "master")
  {
    #Get last Tag from master (because Get-Last-Version-Of-Branch-From-Tag does not reach master, so the master commit could be the most recent)
    $MasterVersion = Get-Last-Version-Of-Branch-From-Tag "master"

    if (-not (Is-Semver $DevelopVersion))
    {
      $MostRecentVersion = $MasterVersion.Substring(1)
    }

    #Take most recent
    $MostRecentVersion = Get-Most-Recent-Version $DevelopVersion.Substring(1) $MasterVersion.Substring(1)
  }
  else
  {
    $MostRecentVersion = $DevelopVersion.Substring(1)
  }

  return $MostRecentVersion
}

function Get-Hotfix-Most-Recent-Version ()
{
  #Get current support branch from name and version of current branch
  $CurrentBranchName = Get-Current-Branchname
  $CurrentBranchFullVersion = Parse-Version-From-BranchName $CurrentBranchName
  $CurrentBranchMajorMinorVersion = Get-Major-Minor-From-Version $CurrentBranchFullVersion
  $CurrentSupportBranchName = "support/v$($CurrentBranchMajorMinorVersion)"

  # fall back to hotfix branch name if no previous release is found on hotfix branch
  if (Get-Last-Version-Of-Branch-From-Tag-Exists "HEAD" $CurrentSupportBranchName)
  {
    #Get version from tag of current branch with highest version
    $MostRecentVersion = (Get-Last-Version-Of-Branch-From-Tag "HEAD" $CurrentSupportBranchName).Substring(1)

    # also fall back to hotfix branch if the found tag is not a pre-release tag (i.e. minor on support branch)
    if ($NULL -ne (Get-PreReleaseStage $MostRecentVersion))
    {
      return $MostRecentVersion
    }
    else
    {
      return $CurrentBranchFullVersion
    }
  }
  else
  {
    return $CurrentBranchFullVersion
  }
}

function Reset-Items-Of-Ignore-List ()
{
  param
  (
    [string]$ListToBeIgnored
  )

  $ConfigFile = Get-Config-File

  $IgnoredFiles = ""
        
  if ($ListToBeIgnored -eq "prereleaseMergeIgnoreList")
  {
    $IgnoredFiles = $ConfigFile.settings.prereleaseMergeIgnoreList.fileName
  }
  elseif ($ListToBeIgnored -eq "tagStableMergeIgnoreList")
  {
    $IgnoredFiles = $ConfigFile.settings.tagStableMergeIgnoreList.fileName
  }
  elseif ($ListToBeIgnored -eq "developStableMergeIgnoreList")
  {
    $IgnoredFiles = $ConfigFile.settings.developStableMergeIgnoreList.fileName
  }

  foreach ($File in $IgnoredFiles)
  {
    if (-Not [string]::IsNullOrEmpty($File) )
    {
      git reset HEAD $File
      git checkout -- $File
    }
  }
}

function Merge-Branch-With-Reset ($CurrentBranchname, $MergeBranchname, $IgnoreList)
{
  git checkout $CurrentBranchname --quiet
  git merge $MergeBranchname --no-ff --no-commit 2>&1 | Write-Host
  Reset-Items-Of-Ignore-List -ListToBeIgnored $IgnoreList
  git commit -m "Merge branch '$($MergeBranchname)' into $($CurrentBranchName)" 2>&1 | Write-Host
  Resolve-Merge-Conflicts
}

function Find-Next-Rc ($CurrentVersion)
{
  $NextRc = Get-Next-Rc $CurrentVersion

  while (Get-Tag-Exists ("v$($NextRc)") )
  {
    $NextRc = Get-Next-Rc $NextRc
  }

  return $NextRc
}

function Create-Tag-And-Merge ()
{
  Check-Is-On-Branch "release/"
    
  $CurrentBranchname = Get-Current-Branchname
  $CurrentVersion = Parse-Version-From-BranchName $CurrentBranchname

  Check-Branch-Up-To-Date $CurrentBranchname
  Check-Branch-Exists-And-Up-To-Date "master"
  Check-Branch-Exists-And-Up-To-Date "develop"
  
  if (Get-Tag-Exists "v$($CurrentVersion)")
  {
    throw "There is already a commit tagged with 'v$($CurrentVersion)'."
  }
    
  git checkout "master" 2>&1 > $NULL
    
  git merge $CurrentBranchname --no-ff 2>&1
    
  Resolve-Merge-Conflicts

  Check-Branch-Up-To-Date "develop"
    
  Merge-Branch-With-Reset "develop" $CurrentBranchname "developStableMergeIgnoreList"

  git checkout master 2>&1 > $NULL
  git tag -a "v$($CurrentVersion)" -m "v$($currentVersion)" 2>&1 > $NULL
    
  git checkout develop 2>&1 | Write-Host
}

function Push-Master-Release ($Version)
{
  $Branchname = "release/v$($Version)"
  $Tagname = "v$($Version)"

  if (-not (Get-Branch-Exists $Branchname) )
  {
    throw "The branch '$($Branchname)' does not exist. Please create a release branch first."
  }

  Check-Branch-Up-To-Date $Branchname
  Push-To-Repos $Branchname

  Check-Branch-Up-To-Date "master"

  Check-Branch-Up-To-Date "develop"
  Push-To-Repos "master" $Tagname
  Push-To-Repos "develop"
}