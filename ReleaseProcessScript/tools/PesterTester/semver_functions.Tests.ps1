. $PSScriptRoot"\..\Core\semver_functions.ps1"
. $PSScriptRoot"\Test_Functions.ps1"

Describe "semver_functions" {
  BeforeEach {
    Set-Alias -Name git -Value (Get-Custom-Git-Path $PSScriptRoot)
  }

  Context "Get-Possible-Next-Versions-Develop" {
    It "Get-Possible-Next-Versions-Develop_WithAlpha_ShouldReturnArray" {
      $Version = "1.2.0-alpha.4"
      $NextVersions = "1.2.0-alpha.5", "1.2.0-beta.1", "1.2.0", "2.0.0-alpha.1", "2.0.0-beta.1", "2.0.0"

      Get-Possible-Next-Versions-Develop $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Develop_WithBeta_ShouldReturnArray" {
      $Version = "1.2.0-beta.2"
      $NextVersions = "1.2.0-beta.3", "1.2.0", "2.0.0-alpha.1", "2.0.0-beta.1", "2.0.0"

      Get-Possible-Next-Versions-Develop $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Develop_WithBetaAndWithoutPrerelease_ShouldReturnArray" {
      $Version = "1.2.0-beta.2"
      $StartReleasePhase = $TRUE

      $NextVersions =  "1.2.0", "2.0.0"

      Get-Possible-Next-Versions-Develop $Version $StartReleasePhase | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Develop_WithRc_ShouldReturnArray" {
      $Version = "1.2.0-rc.4"
      $NextVersions = "1.2.0", "2.0.0-alpha.1", "2.0.0-beta.1", "2.0.0"

      Get-Possible-Next-Versions-Develop $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Develop_WithoutPre_ShouldReturnArray" {
      $Version = "1.2.0"
      $NextVersions = "1.3.0-alpha.1", "1.3.0-beta.1", "1.3.0", "2.0.0", "2.0.0-alpha.1", "2.0.0-beta.1"

      Get-Possible-Next-Versions-Develop $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Develop_OnlyMajor_ShouldReturnArray" {
      $Version = "2.0.0"
      $NextVersions = "2.1.0-alpha.1", "2.1.0-beta.1", "2.1.0", "3.0.0", "3.0.0-alpha.1", "3.0.0-beta.1"

      Get-Possible-Next-Versions-Develop $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Develop_WithPreVersion10_ShouldReturnArray" {
      $Version = "1.0.0-alpha.10"
      $NextVersions = "1.0.0-alpha.11", "1.0.0-beta.1", "1.0.0", "2.0.0-alpha.1", "2.0.0-beta.1", "2.0.0"

      Get-Possible-Next-Versions-Develop $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Develop_WithInvalidVersion_ShouldThrowException" {
      { Get-Possible-Next-Versions-Develop "completelyWrongVersion" } | Should Throw "Your version 'completelyWrongVersion' does not have a valid format (e.g. 1.2.3-alpha.1)"
      { Get-Possible-Next-Versions-Develop "1.2.3.4" } | Should Throw "Your version '1.2.3.4' does not have a valid format (e.g. 1.2.3-alpha.1)"
      { Get-Possible-Next-Versions-Develop "1.2.3-somethinginvalid.4" } | Should Throw "Your version '1.2.3-somethinginvalid.4' does not have a valid format (e.g. 1.2.3-alpha.1)"
      { Get-Possible-Next-Versions-Develop "1.2.3.alpha-4" } | Should Throw "Your version '1.2.3.alpha-4' does not have a valid format (e.g. 1.2.3-alpha.1)"
      { Get-Possible-Next-Versions-Develop "1.2.3.rc.4" } | Should Throw "Your version '1.2.3.rc.4' does not have a valid format (e.g. 1.2.3-alpha.1)"
    }
  }

  Context "Get-Possible-Next-Versions-Hotfix" {
    It "Get-Possible-Next-Versions-Hotfix_WithAlpha_ShouldReturnArray" {
      $Version = "1.2.0-alpha.4"
      $NextVersions = "1.2.0-alpha.5", "1.2.0-beta.1", "1.2.0"

      Get-Possible-Versions-Hotfix $Version $true | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Hotfix_WithBeta_ShouldReturnArray" {
      $Version = "1.2.0-beta.3"
      $NextVersions = "1.2.0-beta.4", "1.2.0"

      Get-Possible-Versions-Hotfix $Version $true | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Hotfix_WithRc_ShouldReturnArray" {
      $Version = "1.2.0-rc.1"
      $NextVersions = "1.2.0-rc.2", "1.2.0"

      Get-Possible-Versions-Hotfix $Version $true | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Hotfix_WithoutPre_ShouldReturnArray" {
      $Version = "1.2.0"
      $NextVersions = "1.2.1", "1.2.1-alpha.1", "1.2.1-beta.1"

      Get-Possible-Versions-Hotfix $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Hotfix_WithAlpha_NotMeantForCurrentVersion_ShouldReturnArray" {
      $Version = "2.28.0-alpha.1"
      $NextVersions = "2.28.0-alpha.2", "2.28.0-beta.1", "2.28.1"

      Get-Possible-Versions-Hotfix $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Hotfix_WithBeta_NotMeantForCurrentVersion_ShouldReturnArray" {
      $Version = "2.28.0-beta.2"
      $NextVersions = "2.28.0-beta.3", "2.28.1"

      Get-Possible-Versions-Hotfix $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Hotfix_WithRc_NotMeantForCurrentVersion_ShouldReturnArray" {
      $Version = "2.28.0-rc.6"
      $NextVersions = "2.28.0-rc.7", "2.28.1"

      Get-Possible-Versions-Hotfix $Version | Should Be $NextVersions
    }

    It "Get-Possible-Next-Versions-Hotfix_WithoutPre_NotMeantForCurrentVersion_ShouldReturnArray" {
      $Version = "2.28.0"
      $NextVersions = "2.28.1", "2.28.1-alpha.1", "2.28.1-beta.1"

      Get-Possible-Versions-Hotfix $Version | Should Be $NextVersions
    }
  }

  Context "Get-Most-Recent-Version" {
    It "Get-Most-Recent-Version_MajorAndMinor" {
      $RecentVersion = "2.1.0-alpha.1"
      $BeforeVersion = "2.0.0"

      Get-Most-Recent-Version $RecentVersion $BeforeVersion | Should Be $RecentVersion
      Get-Most-Recent-Version $BeforeVersion $RecentVersion | Should Be $RecentVersion
    }

    It "Get-Most-Recent-Version_MajorAndMajor" {
      $RecentVersion = "3.0.0"
      $BeforeVersion = "2.0.0"

      Get-Most-Recent-Version $RecentVersion $BeforeVersion | Should Be $RecentVersion
      Get-Most-Recent-Version $BeforeVersion $RecentVersion | Should Be $RecentVersion
    }

    It "Get-Most-Recent-Version_MinorAndMinor" {
      $RecentVersion = "2.1.0"
      $BeforeVersion = "2.0.0"

      Get-Most-Recent-Version $RecentVersion $BeforeVersion | Should Be $RecentVersion
      Get-Most-Recent-Version $BeforeVersion $RecentVersion | Should Be $RecentVersion
    }
  }

  Context "Is-Semver" {
    It "Recognizes valid versions correctly" {
      Is-Semver "1.2.3" | Should Be $TRUE
      Is-Semver "1.2.3-alpha.23" | Should Be $TRUE
      Is-Semver "1.2.3-beta.1" | Should Be $TRUE
      Is-Semver "1.2.3-rc.0" | Should Be $TRUE
    }

    It "Classifies a version with a 'v' prefix as invalid" {
      Is-Semver "v1.2.3" | Should Be $FALSE
    }

    It "Classifies a version without patch part as invalid" {
      Is-Semver "1.2" | Should Be $FALSE
    }

    It "Classifies a version with only major part as invalid" {
      Is-Semver "1" | Should Be $FALSE
    }

    It "Classifies a version with unknown pre-release identifier as invalid" {
      Is-Semver "1.2.3-foobar.2" | Should Be $FALSE
    }

    It "Classifies a version with a fourth group as invalid" {
      Is-Semver "1.2.3.4" | Should Be $FALSE
    }

    It "Classifies a version without a dot separating pre-release identifier from pre-release number as invalid" {
      Is-Semver "1.2.3-alpha4" | Should Be $FALSE
    }
  }
}