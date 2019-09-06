. $PSScriptRoot"\..\Core\main_functions.ps1"
. $PSScriptRoot"\Test_Functions.ps1"
. $PSScriptRoot"\..\Core\config_functions.ps1"
. $PSScriptRoot"\..\Core\jira_functions.ps1"
. $PSScriptRoot"\..\Core\main_helper_functions.ps1"
. $PSScriptRoot"\..\Core\read_functions.ps1"
. $PSScriptRoot"\..\Core\semver_functions.ps1"


#There was an Issue that $PSScriptRoot was null in BeforeEach/AfterEach, so we have to cache it here
$ScriptRoot = $PSScriptRoot

$TestBaseDir = (Get-Item -LiteralPath $env:TEMP).FullName
$TestDir = "$($TestBaseDir)\ReleaseProcessScriptTestRepository"
$ReferenceDir = "$($TestBaseDir)\ReleaseProcessScriptReferenceRepository"

#TODO: Add a MSBuild Step which commits something to test the correct git branching
#TODO: Same with File ignore

Describe "IntegrationTestsTest" {

  BeforeEach {
    #Mock Things which we dont want to test automated in an integration test as they could break something online
    Test-Mock-All-Jira-Functions
    
    #Delete base directory if exists and create it
    if (Test-Path $TestDir) {
      Remove-Item $TestDir -Recurse
    }
    New-Item $TestDir -ItemType directory

    if (Test-Path $ReferenceDir) {
      Remove-Item $ReferenceDir -Recurse
    }
    New-Item $ReferenceDir -ItemType directory

    Initialize-GitRepository $TestDir
    Initialize-GitRepository $ReferenceDir
  }

  AfterEach {
    Set-Location $ScriptRoot
    Remove-Item $TestDir -Recurse -Force
    Remove-Item $ReferenceDir -Recurse -Force
  }

  # Context "ReleaseFromHotfix" {
  #   It "ReleasePatchOnSupport" {
  #     #Add support branch to see if possible other branches do not influence the test run
  #     git checkout -b support/v1.0
  #     git checkout -b hotfix/v1.0.1
  #     git commit --allow-empty -m "Now we have hotfix lingering around somewhere in the log"
  #     { Release-Version } | Should Not Throw
  #   }
  # }

  Context "ReleaseMinorFromDevelop" {
    It "releases a new minor version from develop to master" {
      Initialize-Test "ReleaseMinorFromDevelop"
      Mock Get-Develop-Current-Version { return "1.2.0" }
      Mock Read-Version-Choice { return "1.2.0" }

      { Release-Version } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

#    It "ReleasePrereleaseVersion" {
#      $CurrentPath = "$($ScriptRoot)\TestDirectories\ReleasePrereleaseOnDevelop"
#      git checkout master --quiet
#      git checkout -b develop --quiet

#      Mock Get-Develop-Current-Version { return "1.1.0-alpha.1" }
#      Mock Read-Version-Choice { return "1.2.0" }
     
#      { Release-Version } | Should Not Throw

#      #Compare file structure
#      $CurrentContent = git ls-tree master
#      $ExpectedContent = Get-Content -Path "$($CurrentPath)\fileList.txt"
     
#      $CurrentContent | Should Be $ExpectedContent

#      #Compare commit Trees
#      [string]$CurrentLog = git log Head --graph --pretty=format:'%d %s'
#      [string]$ExpectedLog = Get-Content -Path "$($CurrentPath)\gitLog.txt"

#      $CurrentLog | Should Be $ExpectedLog
#    }
#  }

 
#  Context "ReleaseFromSupport" {
#    It "ReleaseVersionOnSupport" {
#      $CurrentPath = "$($ScriptRoot)\TestDirectories\ReleaseVersionOnSupport"

#      git checkout master --quiet
#      git checkout -b support/v1.1

#      Mock Get-Support-Current-Version { return "1.1.1" }
#      Mock Read-Version-Choice { return "1.2.0" }

#      { Release-Version } | Should Not Throw

#      #Compare file structure
#      $CurrentContent = git ls-tree master
#      $ExpectedContent = Get-Content -Path "$($CurrentPath)\fileList.txt"
     
#      $CurrentContent | Should Be $ExpectedContent

#      #Compare commit Trees
#      [string]$CurrentLog = git log Head --graph --pretty=format:'%d %s'
#      [string]$ExpectedLog = Get-Content -Path "$($CurrentPath)\gitLog.txt"

#      $CurrentLog | Should Be $ExpectedLog
#    }
  }

  Context "ReleaseFromReleasebranch" {
#    It "ReleaseRC" {
#      $CurrentPath = "$($ScriptRoot)\TestDirectories\ReleaseRC"
     
#      git checkout -b develop --quiet
#      git checkout -b release/v1.1.0 --quiet

#      Mock Read-Choice-Of-Two { return 1 }
#      Mock Read-Version-Choice { return "1.2.0" }

#      { Release-Version } | Should Not Throw
     
#      #Compare file structure
#      $CurrentContent = git ls-tree master
#      $ExpectedContent = Get-Content -Path "$($CurrentPath)\fileList.txt"
     
#      $CurrentContent | Should Be $ExpectedContent

#      #Compare commit Trees
#      [string]$CurrentLog = git log Head --graph --pretty=format:'%d %s'
#      [string]$ExpectedLog = Get-Content -Path "$($CurrentPath)\gitLog.txt"

#      $CurrentLog | Should Be $ExpectedLog
#    }

#    It "ReleaseOnMaster" {
#      $CurrentPath = "$($ScriptRoot)\TestDirectories\ReleaseReleaseOnMaster"
   
#      git checkout -b develop --quiet
#      git checkout -b prerelease/v1.1.0-rc.1 --quiet
#      git checkout -b release/v1.1.0 --quiet
   
#      Mock Read-Choice-Of-Two { return 2 }
#      Mock Read-Version-Choice { return "1.2.0" }
   
#      { Release-Version } | Should Not Throw
   
#      #Compare file structure
#      $CurrentContent = git ls-tree master
#      $ExpectedContent = Get-Content -Path "$($CurrentPath)\fileList.txt"
     
#      $CurrentContent | Should Be $ExpectedContent
   
#      #Compare commit Trees
#      [string]$CurrentLog = git log Head --graph --pretty=format:'%d %s'
#      [string]$ExpectedLog = Get-Content -Path "$($CurrentPath)\gitLog.txt"
   
#      $CurrentLog | Should Be $ExpectedLog
#    }

    It "ReleaseOnMasterWithDevelopHeaderNotOnReleaseBranchRoot" {
      Initialize-Test "ReleaseOnMasterWithDevelopHeaderNotOnReleaseBranchRoot"

      Mock Read-Choice-Of-Two { return 2 }
      Mock Read-Version-Choice { return "1.3.0" }

      { Release-Version } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }
  }

  Context "ContinueRelease" {
    It "pauses the release on master" {
      Initialize-Test "ReleaseReleaseOnMasterPauseForCommit"
      Mock Get-Develop-Current-Version { return "1.2.0" }
      Mock Read-Version-Choice {return "1.2.0"}

      { Release-Version -PauseForCommit } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "continues the release after pausing on master" {
      Initialize-Test "ReleaseMinorFromDevelop"
      Mock Get-Develop-Current-Version { return "1.2.0" }
      Mock Read-Version-Choice {return "1.2.0"}

      { Release-Version -PauseForCommit } | Should Not Throw

      git checkout release/v1.2.0 --quiet

      { Continue-Release } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    } 
  }
}
