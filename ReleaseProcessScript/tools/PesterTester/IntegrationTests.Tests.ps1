﻿. $PSScriptRoot"\..\Core\main_functions.ps1"
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

Describe "IntegrationTests" {

  BeforeEach {
    Set-Alias -Name git -Value (Get-Custom-Git-Path $ScriptRoot)
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

    # Show repository in GitExtensions
    # Set-Location $TestDir
    # gitex
    # Set-Location $ScriptRoot

    # Show press key prompt
    # Write-Host -NoNewLine 'Press any key to continue...';
    # $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');

    Remove-Item $TestDir -Recurse -Force
    Remove-Item $ReferenceDir -Recurse -Force
  }

  Context "Hotfix" {
    It "releases a new patch version from hotfix to support" {
      Initialize-Test "Hotfix-PatchRelease"
      Mock Get-Hotfix-Current-Version { return "1.1.1" }
      Mock Read-Version-Choice { return "1.1.2" }

      { Release-Version } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "determines least recently released alpha version correctly from tags" {
      Initialize-Test "Hotfix-PreRelease-Alpha"

      $MostRecentVersion = Get-Hotfix-Most-Recent-Version
      $MostRecentVersion | Should Be "2.28.1-alpha.2"

      $VersionsToRelease = Get-Possible-Versions-Hotfix "2.28.1-alpha.2" $TRUE
      $VersionsToRelease | Should Be "2.28.1-alpha.3", "2.28.1-beta.1", "2.28.1"
    }

    It "determines least recently released beta version correctly from tags" {
      Initialize-Test "Hotfix-PreRelease-Beta"

      $MostRecentVersion = Get-Hotfix-Most-Recent-Version
      $MostRecentVersion | Should Be "2.28.1-beta.1"

      $VersionsToRelease = Get-Possible-Versions-Hotfix "2.28.1-beta.1" $TRUE
      $VersionsToRelease | Should Be "2.28.1-beta.2", "2.28.1"
    }

    It "determines least recently released version correctly from tags, ignoring invalid tags" {
      Initialize-Test "Hotfix-PreRelease-Alpha-WithInvalidTags"

      $MostRecentVersion = Get-Hotfix-Most-Recent-Version

      $MostRecentVersion | Should Be "2.28.1-alpha.1"
    }

    It "falls back to version based on hotfix branch name if no tags are found" {
      Initialize-Test "Hotfix-PatchRelease-NoTags"

      $MostRecentVersion = Get-Hotfix-Most-Recent-Version
      $MostRecentVersion | Should Be "2.28.1"

      $VersionsToRelease = Get-Possible-Versions-Hotfix "2.28.1" $TRUE
      $VersionsToRelease | Should Be "2.28.1", "2.28.1-alpha.1", "2.28.1-beta.1"
    }

    It "falls back to version based on hotfix branch name if no valid tags are found" {
      Initialize-Test "Hotfix-PatchRelease-NoValidTags"

      $CurrentVersion = Get-Hotfix-Most-Recent-Version

      $CurrentVersion | Should Be "2.28.1"
    }

    It "determines versions to be released correctly if no previous prerelease exists" {
      Initialize-Test "Hotfix-PatchRelease-WithoutPriorPreRelease"

      $MostRecentVersion = Get-Hotfix-Most-Recent-Version
      $MostRecentVersion | Should Be "2.27.4"

      $VersionsToRelease = Get-Possible-Versions-Hotfix "2.27.4" $TRUE
      $VersionsToRelease | Should Be "2.27.4", "2.27.4-alpha.1", "2.27.4-beta.1"
    }

    It "falls back to version based on hotfix branch name and ignores minor tag from common support branch fork point" {
      Initialize-Test "Hotfix-PatchRelease-NoPriorRelease-TaggedCommonAncestor"

      $MostRecentVersion = Get-Hotfix-Most-Recent-Version
      $MostRecentVersion | Should Be "2.29.0"

      $VersionsToRelease = Get-Possible-Versions-Hotfix "2.29.0" $TRUE
      $VersionsToRelease | Should Be "2.29.0", "2.29.0-alpha.1", "2.29.0-beta.1"
    }

    It "falls back to version based on hotfix branch name and ignores minor tag on support branch" {
      Initialize-Test "Hotfix-PatchRelease-NoPriorRelease-NoTagOnCommonAncestor"

      $MostRecentVersion = Get-Hotfix-Most-Recent-Version
      $MostRecentVersion | Should Be "2.29.0"

      $VersionsToRelease = Get-Possible-Versions-Hotfix "2.29.0" $TRUE
      $VersionsToRelease | Should Be "2.29.0", "2.29.0-alpha.1", "2.29.0-beta.1"
    }

    It "falls back to version based on hotfix branch name and ignores minor tag on support branch" {
      Initialize-Test "Hotfix-PatchRelease-NoPriorRelease-MinorTag"

      $MostRecentVersion = Get-Hotfix-Most-Recent-Version
      $MostRecentVersion | Should Be "2.29.0"

      $VersionsToRelease = Get-Possible-Versions-Hotfix "2.29.0" $TRUE
      $VersionsToRelease | Should Be "2.29.0", "2.29.0-alpha.1", "2.29.0-beta.1"
    }

    It "determines least recently released alpha version correctly from tags and ignores minor tag from common support branch fork point" {
      Initialize-Test "Hotfix-PreRelease-Alpha-TaggedCommonAncestor"

      $MostRecentVersion = Get-Hotfix-Most-Recent-Version
      $MostRecentVersion | Should Be "2.29.1-alpha.2"

      $VersionsToRelease = Get-Possible-Versions-Hotfix "2.29.1-alpha.2" $TRUE
      $VersionsToRelease | Should Be "2.29.1-alpha.3", "2.29.1-beta.1", "2.29.1"
    }
  }

  Context "Develop" {
    It "releases a new minor version from develop to master" {
      Initialize-Test "Develop-ReleaseMinor"
      Mock Get-Develop-Current-Version { return "1.2.0" }
      Mock Read-Version-Choice { return "1.3.0" }

      { Release-Version } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "releases a pre-release version from develop to release" {
      Initialize-Test "Develop-PreRelease"
      Mock Get-Develop-Current-Version { return "1.2.0-alpha.1" }
      Mock Read-Version-Choice { return "1.2.0" }

      { Release-Version } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "releases a pre-release version from develop to release with a commit on prerelease" {
      Initialize-Test "Develop-PreRelease-WithCommit"
      Mock Get-Develop-Current-Version { return "1.2.0-alpha.1" }
      Mock Read-Version-Choice { return "1.2.0" }

      { Release-Version -PauseForCommit } | Should Not Throw

      git commit -m "Commit on prerelease branch" --allow-empty

      { Continue-Release } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "releases a release version from develop to master with a commit on release" {
      Initialize-Test "Develop-ReleaseMinor-WithCommit"
      Mock Get-Develop-Current-Version { return "1.2.0" }
      Mock Read-Version-Choice { return "1.3.0" }

      { Release-Version -PauseForCommit } | Should Not Throw

      git commit -m "Commit on release branch" --allow-empty

      { Continue-Release } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "determines least recently released version correctly from tags" {
      Initialize-Test "Develop-PreRelease-Beta"

      $MostRecentVersion = Get-Develop-Most-Recent-Version

      $MostRecentVersion | Should Be "2.28.1-beta.1"
    }

    It "determines least recently released version correctly from tags, ignoring invalid tags" {
      Initialize-Test "Develop-PreRelease-Beta-WithInvalidTags"

      $MostRecentVersion = Get-Develop-Most-Recent-Version

      $MostRecentVersion | Should Be "2.28.1-beta.2"
    }
  }

  Context "Support" {
    It "throws an exception when attempting to release from support" {
      Initialize-Test "Support-TryRelease"

      { Release-Version } | Should Throw "You have to be on either a 'hotfix/*' or 'release/*' or 'develop' or 'master' branch to release a version."
    }
  }

  Context "Release" {
    It "releases a release candidate version from release to release" {
      Initialize-Test "Release-RC"
      Mock Read-Choice-Of-Two { return 1 }
      Mock Read-Version-Choice { return "1.2.0" }

      { Release-Version } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "releases a second release candidate version from release to release" {
      Initialize-Test "Release-RC-SecondRC"
      Mock Read-Choice-Of-Two { return 1 }
      Mock Read-Version-Choice { return "1.2.0" }
      Mock Read-Ancestor-Choice { return "release/v1.2.0" }

      { Release-Version -PauseForCommit } | Should Not Throw

      git commit -m "Another commit on prerelease" --allow-empty *>$NULL

      { Continue-Release } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "releases a release candidate version from release to release with a commit on prerelease" {
      Initialize-Test "Release-RC-WithCommit"
      Mock Read-Choice-Of-Two { return 1 }
      Mock Read-Version-Choice { return "1.2.0" }
      Mock Read-Ancestor-Choice { return "release/v1.2.0" }

      { Release-Version -PauseForCommit } | Should Not Throw

      git commit -m "Commit on prerelease branch" --allow-empty

      { Continue-Release } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "releases a release from release to master" {
      Initialize-Test "Release-Release"
      Mock Read-Choice-Of-Two { return 2 }
      Mock Read-Version-Choice { return "1.3.0" }

      { Release-Version } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "releases a release from release to master with an additional commit on develop" {
      Initialize-Test "Release-Release-WithCommitOnDevelop"
      Mock Read-Choice-Of-Two { return 2 }
      Mock Read-Version-Choice { return "1.3.0" }

      { Release-Version } | Should Not Throw

      $TestDirGitLogs = Get-Git-Logs $TestDir
      $ReferenceDirGitLogs = Get-Git-Logs $ReferenceDir

      $TestDirGitLogs | Should Be $ReferenceDirGitLogs
    }

    It "determines least recently released rc version correctly from tags" {
      Initialize-Test "Release-RC-WithAlpha"
    
      $MostRecentVersion = Get-Last-Version-Of-Branch-From-Tag
    
      $MostRecentVersion | Should Be "v2.28.1-rc.1"
    }
  }
}