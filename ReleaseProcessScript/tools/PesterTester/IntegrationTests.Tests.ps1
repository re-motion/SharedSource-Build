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

    Set-Location $TestDir
    gitex
    Set-Location $ScriptRoot

    Remove-Item $TestDir -Recurse -Force
    Remove-Item $ReferenceDir -Recurse -Force
  }

  Context "ReleasePatchFromHotfix" {
    # It "releases a new patch version from hotfix to support" {
    #   Initialize-Test "ReleasePatchFromHotfix"
    #   Mock Get-Hotfix-Current-Version { return "1.1.1" }
    #   Mock Read-Version-Choice { return "1.1.2" }

    #   { Release-Version } | Should Not Throw

    #   $TestDirGitLogs = Get-Git-Logs $TestDir
    #   $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

    #   $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    # }
  }

  Context "ReleaseFromDevelop" {
    # It "releases a new minor version from develop to master" {
    #   Initialize-Test "ReleaseMinorFromDevelop"
    #   Mock Get-Develop-Current-Version { return "1.2.0" }
    #   Mock Read-Version-Choice { return "1.3.0" }

    #   { Release-Version } | Should Not Throw

    #   $TestDirGitLogs = Get-Git-Logs $TestDir
    #   $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

    #   $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    # }

    It "releases a pre-release version from develop" {
      Initialize-Test "ReleasePrereleaseOnDevelop"
      Mock Get-Develop-Current-Version { return "1.2.0-alpha.1" }
      Mock Read-Version-Choice { return "1.2.0" }

      { Release-Version } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "releases a pre-release version from develop with commit on prerelease" {
      Initialize-Test "ReleasePrereleaseOnDevelopWithCommit"
      Mock Get-Develop-Current-Version { return "1.2.0-alpha.1" }
      Mock Read-Version-Choice { return "1.2.0" }

      { Release-Version -PauseForCommit } | Should Not Throw

      git commit -m "Commit on prerelease branch" --allow-empty

      { Continue-Release } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }
  }

  Context "ReleaseFromSupport" {
    # It "throws an exception when attempting to release from support branch" {
    #   Initialize-Test "ReleaseVersionOnSupport"

    #   { Release-Version } | Should Throw "You have to be on either a 'hotfix/*' or 'release/*' or 'develop' or 'master' branch to release a version."
    # }
  }

  Context "ReleaseFromReleasebranch" {
    # It "releases a release candidate version from a release branch" {
    #   Initialize-Test "ReleaseRC"
    #   Mock Read-Choice-Of-Two { return 1 }
    #   Mock Read-Version-Choice { return "1.2.0" }

    #   { Release-Version } | Should Not Throw

    #   $TestDirGitLogs = Get-Git-Logs $TestDir
    #   $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

    #   $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    # }

    # It "ReleaseOnMaster" {
    #   Initialize-Test "ReleaseReleaseOnMaster"
    #   Mock Read-Choice-Of-Two { return 2 }
    #   Mock Read-Version-Choice { return "1.3.0" }

    #   { Release-Version } | Should Not Throw

    #   $TestDirGitLogs = Get-Git-Logs $TestDir
    #   $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

    #   $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    # }

    # It "ReleaseOnMasterWithDevelopHeaderNotOnReleaseBranchRoot" {
    #   Initialize-Test "ReleaseOnMasterWithDevelopHeaderNotOnReleaseBranchRoot"
    #   Mock Read-Choice-Of-Two { return 2 }
    #   Mock Read-Version-Choice { return "1.3.0" }

    #   { Release-Version } | Should Not Throw

    #   $TestDirGitLogs = Get-Git-Logs $TestDir
    #   $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

    #   $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    # }
  }

  Context "ContinueRelease" {
    # It "pauses the release on master" {
    #   Initialize-Test "ReleaseReleaseOnMasterPauseForCommit"
    #   Mock Get-Develop-Current-Version { return "1.2.0" }
    #   Mock Read-Version-Choice { return "1.3.0" }

    #   { Release-Version -PauseForCommit } | Should Not Throw

    #   $TestDirGitLogs = Get-Git-Logs $TestDir
    #   $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

    #   $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    # }

    # It "continues the release after pausing on master" {
    #   Initialize-Test "ReleaseMinorFromDevelop"
    #   Mock Get-Develop-Current-Version { return "1.2.0" }
    #   Mock Read-Version-Choice { return "1.3.0" }

    #   { Release-Version -PauseForCommit } | Should Not Throw

    #   git checkout release/v1.2.0 --quiet

    #   { Continue-Release } | Should Not Throw

    #   $TestDirGitLogs = Get-Git-Logs $TestDir
    #   $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

    #   $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    # }
  }
}