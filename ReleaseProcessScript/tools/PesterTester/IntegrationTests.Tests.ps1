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

    # # Show repository in GitExtensions
    # Set-Location $TestDir
    # gitex
    # Set-Location $ScriptRoot

    # # Show press key prompt
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
  }
}