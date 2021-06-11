. $PSScriptRoot"\config_functions.ps1"
. $PSScriptRoot"\semver_functions.ps1"
. $PSScriptRoot"\read_functions.ps1"

function Get-Branch-Exists ($Branchname)
{
  return (git show-ref --verify -- "refs/heads/$($Branchname)" 2>&1) -and $?
}

function Get-Branch-Exists-Remote ($RemoteName, $Branchname)
{
  return (git ls-remote --heads $RemoteName $Branchname 2>&1) -and $?
}

function Get-Tag-Exists ($Tagname)
{
  return (git show-ref --verify -- "refs/tags/$($Tagname)" 2>&1) -and $?
}

function Get-Current-Branchname ()
{
  return $(git symbolic-ref --short -q HEAD)
}

function Get-Last-Version-Of-Branch-From-Tag-Exists
(
  $From, #A commit-ish from which to start looking for version tags (backwards in history). Can be a tag name, a branch name, a commit hash, HEAD etc. Default: HEAD
  $To #A commit-ish until which version tag are looked for. Can be a tag name, a branch name, a commit hash, HEAD etc. Default: "unbounded"
)
{
  $LastVersion = Get-Last-Version-Of-Branch-From-Tag $From $To
  return $LastVersion -and ($LastVersion.Length -gt 0)
}

function Get-Last-Version-Of-Branch-From-Tag
(
  $From, #A commit-ish from which to start looking for version tags (backwards in history). Can be a tag name, a branch name, a commit hash, HEAD etc. Default: HEAD
  $To #A commit-ish until which version tag are looked for. Can be a tag name, a branch name, a commit hash, HEAD etc. Default: "unbounded"
)
{
  $ValidVersions = @(Get-Valid-Versions-From-Tags-Sorted $From $To)
  return $ValidVersions[0]
}

function Get-Valid-Versions-From-Tags-Sorted
(
  $From, #A commit-ish from which to start looking for version tags (backwards in history). Can be a tag name, a branch name, a commit hash, HEAD etc. Default: HEAD
  $To #A commit-ish until which version tag are looked for. Can be a tag name, a branch name, a commit hash, HEAD etc. Default: "unbounded"
)
{
  #The tags are determined by gathering all tags reachable from $From to $To. They are sorted decreasingly
  #according to semver rules, which is why the the latest version comes out as first element of the result array.

  #pre-release versions are sorted in order of the specification of the suffixes below
  #more information at: https://git-scm.com/docs/git-config/2.19.2#Documentation/git-config.txt-versionsortsuffix
  $GitParams = @("-c", "versionsort.suffix=-alpha", "-c", "versionsort.suffix=-beta", "-c", "versionsort.suffix=-rc")

  #--sort=-version:refname makes git treat tag names as versions and sort in reverse order (hence the "-")
  #so that the latest version is on top of the result list; see also parameter descriptions at https://git-scm.com/docs/git-for-each-ref
  $RefParams = @("refs/tags", "--sort=-version:refname", "--format=`"%(refname:short)`"")

  #version tags are looked for starting from revision $From (backwards in history) until $To (empty string is equivalent to "unbounded")
  $RefParams += If ([string]::IsNullOrEmpty($From)) {"--merged=HEAD"} Else {"--merged=`"$From`""}
  $RefParams += If ([string]::IsNullOrEmpty($To)) {""} Else {"--contains=`"$To`""}

  #we are only interested in version tags that start with a "v" and are valid according to our defintion of semver versions
  $AllVersions = @(git $GitParams for-each-ref $RefParams)
  $ValidVersions = @($AllVersions | Where-Object -FilterScript {($_.StartsWith("v")) -and (Is-Semver $_.Substring(1))})

  return $ValidVersions
}

function Pre-Release-Version-Exists
(
  $From, #A commit-ish from which to start looking for version tags (backwards in history). Can be a tag name, a branch name, a commit hash, HEAD etc. Default: HEAD
  $To #A commit-ish until which version tag are looked for. Can be a tag name, a branch name, a commit hash, HEAD etc. Default: "unbounded"
)
{
  $ValidVersions = @(Get-Valid-Versions-From-Tags-Sorted $From $To)

  if ($NULL -ne $ValidVersions)
  {
    foreach ($Version in $ValidVersions)
    {
      $PreReleaseStage = Get-PreReleaseStage $Version.Substring(1)
      if ($NULL -ne $PreReleaseStage)
      {
        return $TRUE
      }
    }
  }

  return $FALSE
}

function Is-On-Branch ($Branchname)
{
	$SymbolicRef = $(git symbolic-ref --short -q HEAD)

  if ($SymbolicRef -eq $Branchname)
  {
    return $TRUE
  }

  if ($Branchname.EndsWith("/") -and $SymbolicRef.StartsWith($Branchname))
  {
    return $TRUE
  } 

	return $FALSE
}

function Push-To-Repos ($Branchname, $Tagname)
{
  $ConfigFile = Get-Config-File

  #Config file should probably be loaded if Release-Version was called before. If not we try if there is a correct config File in the current Branch
  if ($ConfigFile -eq $NULL)
  {
    Load-Config-File
  }
  $ConfigFile = Get-Config-File

  $BeforeBranchname = Get-Current-Branchname
  git checkout $Branchname 2>&1 --quiet

  if ( (-not [string]::IsNullOrEmpty($Tagname)) -and (-not (Get-Tag-Exists $Tagname)))
  {
    throw "Tag with name '$($Tagname)' does not exist, abort pushing branch and tag."
  }

  $RemoteNames = $ConfigFile.settings.remoteRepositories.remoteName
  $GitConfigRemotes = Get-Config-Remotes-Array

  foreach ($RemoteName in $RemoteNames)
  {
    if (-not [string]::IsNullOrEmpty($RemoteName) )
    {
      $SetUpstream = [string]::Empty

      $RemoteNameOfBranch = Get-Remote-Of-Branch $Branchname

      #Our Branch has no tracking reference defined (probably a new branch)
      if ([string]::IsNullOrEmpty($RemoteNameOfBranch) )
      {
        $WithoutAsking = $TRUE
        $Ancestor = Get-First-Ancestor

        $RemoteNameOfBranch = $NULL

        if ($Ancestor -ne $NULL)
        {
          $RemoteNameOfAncestor = Get-Remote-Of-Branch $Ancestor
        } 
          
        #And we also cant find an Ancestor with a tracking reference defined
        if ([string]::IsNullOrEmpty($RemoteNameOfAncestor) )
        {
          #Just assign the tracking reference to the first Remote defined in the releaseprocessscript.config
          if ($RemoteName -eq $RemoteNames[0])
          {
            $SetUpstream = "-u"
          }
        }
        elseif ($RemoteNameOfBranch -eq $RemoteName)
        {
            $SetUpstream = "-u"
        }
      } 

      & git push $SetUpstream $RemoteName $Branchname $Tagname 2>&1 | Write-Host
    }
  }
    
  git checkout $BeforeBranchname 2>&1 --quiet
}

function Get-Config-Remotes-Array ()
{
  #remote.<remotename>.url <remoteurl>
  $GitConfigRemoteUrls = git config --get-regexp remote.*.url
  $SplitGitConfigRemoteUrls = $NULL

  #Split entries into an array containing alternately remote.<remotename>.url and <remoteurl>
  if (-not [string]::IsNullOrEmpty($GitConfigRemoteUrls))
  {
    $SplitGitConfigRemoteUrls = $GitConfigRemoteUrls.Split().Split()
  }

  return $SplitGitConfigRemoteUrls
}

function Check-Branch-Up-To-Date($Branchname)
{
  git checkout $Branchname --quiet

  $ConfigFile = Get-Config-File
  $RemoteNames = $ConfigFile.settings.remoteRepositories.remoteName

  $GitConfigRemotes = Get-Config-Remotes-Array

  foreach ($RemoteName in $RemoteNames)
  {
    if (-not [string]::IsNullOrEmpty($RemoteName))
    {
      if (-Not (Get-Branch-Exists-Remote $Remotename $Branchname) )
      {
        continue 
      }

      git fetch $RemoteName $Branchname 2>&1 | Write-Host 

      $Local = $(git rev-parse $Branchname)
      $Remote = $(git rev-parse "$($RemoteName)/$($Branchname)")
      $Base = $(git merge-base $Branchname "$($RemoteName)/$($Branchname)")

      if ($Local -eq $Remote)
      {
        #Up-to-Date. OK
      } 
      elseif ($Local -eq $Base)
      {
        throw "Need to pull, local '$($Branchname)' branch is behind on repository '$($RemoteName)'."
      } 
      elseif ($Remote -eq $Base)
      {
        #Need to push, remote branch is behind. OK
      } 
      else
      {
        throw "'$($Branchname)' diverged, need to rebase at repository '$($RemoteName)'."
      }
    }
  }
}

function Check-Branch-Merged ($Branch, $PossiblyMergedBranchName)
{
  git checkout $Branch --quiet
    
  $MergedBranches = git branch --merged 2>&1 | Out-String
    
  if (-not [string]::IsNullOrEmpty($MergedBranches))
  {
    if ($MergedBranches -like $PossiblyMergedBranchName)
    {
      return $TRUE
    }
  }

  return $FALSE
}

function Resolve-Merge-Conflicts ()
{
  $MergeConflicts = git ls-files -u | git diff --name-only --diff-filter=U
    
  if (-not [string]::IsNullOrEmpty($MergeConflicts))
  {
    git mergetool $MergeConflicts
    git commit --file .git/MERGE_MSG
  }
}

function Is-Working-Directory-Clean ()
{
  $Status = git status --porcelain

  if ([string]::IsNullOrEmpty($Status))
  {
    return $TRUE
  } 
  else
  {
    return $FALSE
  }

  return $FALSE
}

function Get-Branch-From-Hash ($CommitHash)
{
  $Branches = git branch --contains $CommitHash

  $SplitBranches = $Branches.Split()

  if ($SplitBranches.Count -ne 2)
  {
    throw "Commit hash '$($CommitHash)' is contained in more than one branch or in none."
  }

  return $SplitBranches[1]
}

function Check-Commit-Hash ($CommitHash)
{
  if (-not $CommitHash)
  {
    return
  }

  $HashValidation = git cat-file -t $CommitHash

  if ($HashValidation -ne "commit")
  {
    throw "Given commit hash '$($CommitHash)' not found in repository."
  }
}

function Get-Remote-Of-Branch ($Branchname)
{
  return git config "branch.$($Branchname).remote"
}

function Get-First-Ancestor ()
{
  $Branchname = Get-Current-Branchname
  
  $Ancestor = git show-branch | Where-Object { $_.Contains('*') -eq $TRUE } | Where-Object { $_.Contains($Branchname) -ne $TRUE } | Select -First 1 | % {$_ -replace('.*\[(.*)\].*','$1')} | % { $_ -replace('[\^~].*','') }
  
  return $Ancestor
}


function Get-Ancestor ($ExpectedAncestors)
{
  $Branchname = Get-Current-Branchname
  
  $Ancestors = git show-branch | Where-Object { $_.Contains('!') -eq $TRUE } | Where-Object { $_.Contains($Branchname) -ne $TRUE } | % {$_ -replace('.*\[(.*)\].*','$1')} | % { $_ -replace('[\^~].*','') }

  [System.Collections.ArrayList]$FoundAncestors = @()
  foreach ($Ancestor in $Ancestors) 
  {
    if ($ExpectedAncestors.Contains($Ancestor) -or ($ExpectedAncestors | ForEach-Object { return $Ancestor.StartsWith($_) }) -Contains $true)
    {
      $FoundAncestors.Add($Ancestor) | Out-Null
    }
  }

  if ($FoundAncestors.Count -eq 0)
  {
    $JointExpectedAncestors = "'$($ExpectedAncestors -join "','")'"
    return Read-Host "We expected some of the following Ancestors: [$JointExpectedAncestors] but found none. Please enter the name of the Ancestor Branch:"
  }

  if ($FoundAncestors.Count -eq 1)
  {
    return $FoundAncestors[0]
  }

  return Read-Ancestor-Choice $ExpectedAncestors $FoundAncestors
}

function Get-BranchHeads-On-Head ()
{
  #Get all Branches which point to Head
  return git for-each-ref --format="%(refname:short)" 'refs/heads' --points-at HEAD
}

function Check-Min-Git-Version () 
{
  $MinimalGitVersion = "2.7.0"
  $CurrentGitVersion = git version

  $VersionRegex = [regex]"\d+(\.\d+)+"
  
  $Matches = $VersionRegex.Match($CurrentGitVersion)

  #Min Version is 2.7.0 as "git for-each-ref --points-at" got introduced in this version
  if ($Matches.Success -and [Version]$Matches.Captures[0].value -ge [Version]$MinimalGitVersion)
  {
    return
  }
  else
  {
    throw "Git Version '$($MinimalGitVersion)' or higher is required. Current Git Version: '$($CurrentGitVersion)'."
  }
}