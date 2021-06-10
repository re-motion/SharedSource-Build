. $PSScriptRoot"\..\Core\git_base_functions.ps1"
. $PSScriptRoot"\Test_Functions.ps1"

#There was an Issue that $PSScriptRoot was null in BeforeEach/AfterEach, so we have to cache it here
$ScriptRoot = $PSScriptRoot

$TestBaseDir = "C:\temp"
$TestDirName = "GitUnitTestDir"
$PseudoRemoteTestDir = "RemoteTestDir"
$TestRemoteName = "testRemote"

Describe "git_base_functions" {
  BeforeEach {
    Set-Alias -Name git -Value (Get-Custom-Git-Path $ScriptRoot)
    cd $ScriptRoot
    [xml]$ConfigFile = Get-Content "releaseProcessScript.config"

    $OldRemoteNameNodes = $ConfigFile.SelectNodes("//remoteName")
    foreach ($Node in $OldRemoteNameNodes)
    {
      $ConfigFile.settings.remoteRepositories.RemoveChild($Node)
    }

    $RemoteNameNode = $ConfigFile.CreateElement("remoteName")
    $ConfigFile.SelectSingleNode("//remoteRepositories").AppendChild($RemoteNameNode)
    $ConfigFile.settings.remoteRepositories.remoteName = $TestRemoteName

    Mock Get-Config-File { return $ConfigFile }

    Test-Create-Repository "$($TestBaseDir)\\$($TestDirName)"
    cd "$($TestBaseDir)\\$($TestDirName)"

    if(-not $?)
    {
      #cd failed, we dont want the test to run
      throw "cd command failed, abort test"
    }
  }

  AfterEach {
    cd $TestBaseDir
    Remove-Item -Recurse -Force $TestDirName 2>&1 | Out-Null
    Remove-Item -Recurse -Force $PseudoRemoteTestDir 2>&1 | Out-Null
    cd $ScriptRoot
  }

  Context "Get-Branch-Exists" {
    It "Get-Branch-Exists_BranchExists_ReturnTrue" {
      git checkout -b "newBranch" --quiet
        
      Get-Branch-Exists "newBranch" | Should Be $TRUE
    }
       
    It "Get-Branch-Exists_BranchDoesNotExists_ReturnFalse" {
      Get-Branch-Exists "notExistingBranch" | Should Be $FALSE
    }
  }

  Context "Get-Branch-Exists-Remote" {
    It "Get-Branch-Exists-Remote_BranchExists_ReturnTrue" {
      git checkout -b "newBranch" --quiet
      Test-Add-Commit
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir
      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"

      Get-Branch-Exists-Remote "$($TestBaseDir)\$($TestDirName)" "newBranch" | Should Be $TRUE
    }

    It "Get-Branch-Exists-Remote_BranchDoesNotExists_ReturnFalse" {
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir
      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"
      git checkout -b "notExistingOnRemote" --quiet


      Get-Branch-Exists-Remote "$($TestBaseDir)\\$($TestDirName)" "notExistingOnRemote" | Should Be $FALSE
    }
  }

  Context "Get-Last-Version-Of-Branch-From-Tag" {
    It "determines latest version correctly with only valid tags" {
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.4" "master"
      Test-Release-Version "v1.2.5" "master"

      $LatestVersion = Get-Last-Version-Of-Branch-From-Tag

      $LatestVersion | Should Be "v1.2.5"
    }

    It "determines latest version correctly with only valid tags, including prereleases" {
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.4-alpha.1" "master"
      Test-Release-Version "v1.2.4-beta.1" "master"
      Test-Release-Version "v1.2.4-beta.2" "master"
      Test-Release-Version "v1.2.4-rc.1" "master"

      $LatestVersion = Get-Last-Version-Of-Branch-From-Tag

      $LatestVersion | Should Be "v1.2.4-rc.1"
    }

    It "determines latest version correctly, ignoring invalid tags" {
      Test-Release-Version "v1.2.2" "master"
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "vv1.2.4" "master" # invalid
      Test-Release-Version "v1.3" "master" # invalid
      Test-Release-Version "1.2.4" "master" # invalid
      Test-Release-Version "1.2.3.4" "master" # invalid
      Test-Release-Version "v1.2.3.4" "master" # invalid

      $LatestVersion = Get-Last-Version-Of-Branch-From-Tag

      $LatestVersion | Should Be "v1.2.3"
    }

    It "determines latest version correctly, including prereleases, ignoring invalid tags" {
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.4-alpha1" "master" # invalid
      Test-Release-Version "v1.2.4-beta.1" "master"
      Test-Release-Version "v1.2.4-beta.2" "master"
      Test-Release-Version "1.2.4-beta.3" "master" # invalid
      Test-Release-Version "v1.2.4-foobar.1" "master" # invalid

      $LatestVersion = Get-Last-Version-Of-Branch-From-Tag

      $LatestVersion | Should Be "v1.2.4-beta.2"
    }
  }

  Context "Get-Last-Version-Of-Branch-From-Tag-Exists" {
    It "determines that there is no latest version without prior release" {
      $LatestVersionExists = Get-Last-Version-Of-Branch-From-Tag-Exists

      $LatestVersionExists | Should Be $FALSE
    }

    It "determines that there is a latest version with prior release" {
      Test-Release-Version "v1.2.3" "master"

      $LatestVersionExists = Get-Last-Version-Of-Branch-From-Tag-Exists

      $LatestVersionExists | Should Be $TRUE
    }

    It "determines that there is no latest version with only invalid tags" {
      Test-Release-Version "v1.3" "master" # invalid
      Test-Release-Version "1.2.4" "master" # invalid
      Test-Release-Version "1.2.3.4" "master" # invalid
      Test-Release-Version "v1.2.3.4" "master" # invalid
      Test-Release-Version "vv1.2.3" "master" # invalid

      $LatestVersionExists = Get-Last-Version-Of-Branch-From-Tag-Exists

      $LatestVersionExists | Should Be $FALSE
    }

    It "determines that there is a latest version despite invalid tags" {
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.3" "master" # invalid
      Test-Release-Version "1.2.4" "master" # invalid

      $LatestVersionExists = Get-Last-Version-Of-Branch-From-Tag-Exists

      $LatestVersionExists | Should Be $TRUE
    }

    It "determines that there is no latest version with only invalid prerelease tags" {
      Test-Release-Version "v1.2.4-alpha1" "master" # invalid
      Test-Release-Version "1.2.4-beta.1" "master" # invalid
      Test-Release-Version "v1.2.4-foobar.1" "master" # invalid

      $LatestVersionExists = Get-Last-Version-Of-Branch-From-Tag-Exists

      $LatestVersionExists | Should Be $FALSE
    }

    It "determines that there is a latest version despite invalid prerelease tags" {
      Test-Release-Version "v1.2.4-alpha1" "master" # invalid
      Test-Release-Version "1.2.4-beta.1" "master" # invalid
      Test-Release-Version "v1.2.4-rc.1" "master"

      $LatestVersionExists = Get-Last-Version-Of-Branch-From-Tag-Exists

      $LatestVersionExists | Should Be $TRUE
    }
  }

  Context "Get-Valid-Versions-From-Tags-Sorted" {
    It "determines that there are no released versions without prior release" {
      $ValidReleaseTags = Get-Valid-Versions-From-Tags-Sorted

      $ValidReleaseTags | Should BeNullOrEmpty
    }

    It "determines release tags after alpha and hotfix release" {
      Test-Release-Version "v1.2.3-alpha.1" "master"
      Test-Release-Version "v1.2.3.0-alpha.2" "master" # invalid, ignored
      Test-Release-Version "v1.2.3" "master"

      $ValidReleaseTags = Get-Valid-Versions-From-Tags-Sorted

      $ValidReleaseTags | Should Be "v1.2.3", "v1.2.3-alpha.1"
    }

    It "determines released versions in correct semver order" {
      Test-Release-Version "v1.0.0-alpha.-1" "master" # invalid, ignored
      Test-Release-Version "v1.0.0-alpha.0" "master"
      Test-Release-Version "v1.0.0-alpha.2" "master"
      Test-Release-Version "v1.0.0-beta.1" "master"
      Test-Release-Version "v1.0.0-beta.11" "master"
      Test-Release-Version "v1.0.0-rc.1" "master"
      Test-Release-Version "v1.0.0" "master"

      $ValidReleaseTags = Get-Valid-Versions-From-Tags-Sorted

      $ValidReleaseTags | Should Be "v1.0.0", "v1.0.0-rc.1", "v1.0.0-beta.11", "v1.0.0-beta.1", "v1.0.0-alpha.2", "v1.0.0-alpha.0"
    }

    It "determines that versions with invalid tags in between are still recognized" {
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.4-alpha1" "master" # invalid
      Test-Release-Version "1.2.4-beta.1" "master" # invalid
      Test-Release-Version "v1.2.4-rc.1" "master"

      $ValidReleaseTags = Get-Valid-Versions-From-Tags-Sorted

      $ValidReleaseTags | Should Be "v1.2.4-rc.1", "v1.2.3"
    }

    It "respects the lower boundary of version tags to consider correctly" {
      Test-Release-Version "v1.2.3" "master"
      $LowerBound = Test-Release-Version "v1.2.4-alpha.1" "master"
      Test-Release-Version "v1.2.4-alpha.2" "master"
      Test-Release-Version "v1.2.4" "master"

      # look for tags from tip of master (HEAD would be equivalent) backwards in the commit history until revision $LowerBound (exclusive)
      $ValidReleaseTagsInRange = Get-Valid-Versions-From-Tags-Sorted "master" $LowerBound

      $ValidReleaseTagsInRange | Should Be "v1.2.4", "v1.2.4-alpha.2"
    }

    It "respects the upper boundary of version tags to consider correctly" {
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.4-alpha.1" "master"
      $UpperBound = Test-Release-Version "v1.2.4-alpha.2" "master"
      Test-Release-Version "v1.2.4" "master"

      # look for tags from $UpperBound backwards in the commit history without lower bound
      $ValidReleaseTagsInRange = Get-Valid-Versions-From-Tags-Sorted $UpperBound

      $ValidReleaseTagsInRange | Should Be "v1.2.4-alpha.2", "v1.2.4-alpha.1", "v1.2.3"
    }

    It "respects the lower and upper boundary of version tags to consider correctly" {
      $LowerBound = Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.4-alpha.1" "master"
      $UpperBound = Test-Release-Version "v1.2.4-alpha.2" "master"
      Test-Release-Version "v1.2.4" "master"

      # look for tags between $UpperBound and $LowerBound (exlusive)
      $ValidReleaseTagsInRange = Get-Valid-Versions-From-Tags-Sorted $UpperBound $LowerBound

      $ValidReleaseTagsInRange | Should Be "v1.2.4-alpha.2", "v1.2.4-alpha.1"
    }
  }

  Context "Pre-Release-Version-Exists" {
    It "determines that there are no pre-release versions without any prior release" {
      $PreReleaseExists = Pre-Release-Version-Exists

      $PreReleaseExists | Should Be $FALSE
    }

    It "determines there is a prerelease after alpha and hotfix release" {
      Test-Release-Version "v1.2.3-alpha.1" "master"
      Test-Release-Version "v1.2.3.0-alpha.2" "master" # invalid, ignored
      Test-Release-Version "v1.2.3" "master"

      $PreReleaseExists = Pre-Release-Version-Exists

      $PreReleaseExists | Should Be $TRUE
    }

    It "determines there is a prerelease after RC release" {
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.3.0-alpha.2" "master" # invalid, ignored
      Test-Release-Version "v1.2.4-rc.1" "master"

      $PreReleaseExists = Pre-Release-Version-Exists

      $PreReleaseExists | Should Be $TRUE
    }

    It "determines there is a prerelease after beta release" {
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.3.0-alpha.2" "master" # invalid, ignored
      Test-Release-Version "v1.2.4-beta.5" "master"

      $PreReleaseExists = Pre-Release-Version-Exists

      $PreReleaseExists | Should Be $TRUE
    }

    It "determines there is no prerelease version after only patch releases" {
      Test-Release-Version "v1.0.0" "master"
      Test-Release-Version "v1.0.1" "master"
      Test-Release-Version "v1.0.2" "master"
      Test-Release-Version "v1.1.1" "master"
      Test-Release-Version "v1.1.2" "master"

      $PreReleaseExists = Pre-Release-Version-Exists

      $PreReleaseExists | Should Be $FALSE
    }

    It "determines there is no prerelease version when having set lower and upper bounds to exlude prereleases" {
      Test-Release-Version "v1.2.3-alpha.1" "master"
      $LowerBound = Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.4" "master"
      $UpperBound = Test-Release-Version "v1.2.5" "master"
      Test-Release-Version "v1.2.6-beta.1" "master"
      Test-Release-Version "v1.2.6" "master"

      # look for tags between $UpperBound and $LowerBound
      $PreReleaseExists = Pre-Release-Version-Exists $UpperBound $LowerBound

      $PreReleaseExists | Should Be $FALSE
    }

    It "determines there is a prerelease version when having set lower and upper bounds to exlude all but one prereleases" {
      Test-Release-Version "v1.2.3" "master"
      Test-Release-Version "v1.2.4-alpha.1" "master"
      $LowerBound = Test-Release-Version "v1.2.4" "master"
      Test-Release-Version "v1.2.5" "master"
      Test-Release-Version "v1.2.6-beta.1" "master"
      $UpperBound = Test-Release-Version "v1.2.6" "master"

      # look for tags between $UpperBound and $LowerBound
      $PreReleaseExists = Pre-Release-Version-Exists $UpperBound $LowerBound

      $PreReleaseExists | Should Be $TRUE
    }
  }

  Context "Get-Tag-Exists" {
    It "Get-Tag-Exists_TagExists_ReturnTrue" {
      git tag -a "newTag" -m "newTag" 2>&1 > $NULL

      Get-Tag-Exists "newTag" | Should Be $TRUE
    }
        
    It "Get-Tag-Exists_TagDoesNotExist_ReturnFalse" {
      Get-Tag-Exists "notExistingTag" | Should Be $FAlSE
    }
  }

  Context "Is-On-Branch" {
    It "Is-On-Branch_OnRightBranch_ReturnTrue" {
      Is-On-Branch "master" | Should Be $TRUE
    }

    It "Is-On-Branch_NotOnRightBranch_ReturnFalse" {
      Is-On-Branch "randomBranch" | Should Be $FALSE
    }

    It "Is-On-Branch-OnSubBranch_ReturnTrue" {
      git checkout -b "support/test" 2>&1 | Out-Null

      Is-On-Branch "support/" | Should Be $TRUE
    }

    It "Is-On-Branch_OnSubBranch_CalledWithoutSlash_ReturnFalse" {
      git checkout -b "support/test" 2>&1 | Out-Null

      Is-On-Branch "support" | Should Be $FALSE
    }
  }

  Context "Check-Branch-Up-To-Date" {
    It "Check-Branch-Up-To-Date_BrancheBehind_ShouldThrowException" {
      git checkout master --quiet
      Test-Add-Commit
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir

      git checkout master --quiet
      Test-Add-Commit

      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"
      git checkout master --quiet
            
      $RemoteUrl = "$($TestBaseDir)\$($TestDirName)"
      git remote add $TestRemoteName $RemoteUrl

      { Check-Branch-Up-To-Date "master" } | Should Throw "Need to pull, local 'master' branch is behind on repository '$($TestRemoteName)'."
    }

    It "Check-Branch-Up-To-Date_BranchesDiverged_ShouldThrowException" {
      git checkout master --quiet
      Test-Add-Commit
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir

      git checkout master --quiet
      Test-Add-Commit "--amend"


      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"
      git checkout master --quiet
            
      $RemoteUrl = "$($TestBaseDir)\$($TestDirName)"
      git remote add $TestRemoteName $RemoteUrl

      { Check-Branch-Up-To-Date "master" } | Should Throw "'master' diverged, need to rebase at repository '$($TestRemoteName)'."
    }

    It "Check-Branch-Up-To-Date_RemoteBranchBehind_ShouldNotThrow" {
      git checkout master --quiet
      Test-Add-Commit
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir

      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"
      git checkout master --quiet
      Test-Add-Commit
          
      $RemoteUrl = "$($TestBaseDir)\$($TestDirName)"
      git remote add $TestRemoteName $RemoteUrl
          
      { Check-Branch-Up-To-Date "master" } | Should Not Throw 
    }

    It "Check-Branch-Up-To-Date_BranchesEqual_ShouldNotThrow" {
      git checkout master --quiet
      Test-Add-Commit
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir

      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"
      git checkout master --quiet

      $RemoteUrl = "$($TestBaseDir)\$($TestDirName)"

      git remote add $TestRemoteName $RemoteUrl

      { Check-Branch-Up-To-Date "master" } | Should Not Throw 
    }
  }

  Context "Get-Ancestor" {
    It "should find the correct ancestor even if only the start of the ancestor is passed" {
      git checkout -b "newBranch" --quiet
      $ExpectedAncestors = "mast", "someOtherBranch"

      Get-Ancestor $ExpectedAncestors | Should Be "master"
    }

    It "should print all found ancestors if multiple are found" {
      git checkout -b "additionalAncestor" --quiet
      git checkout -b "newBranch" --quiet
      $ExpectedAncestors = "master", "additionalAncestor"
      Mock Write-Host -ParameterFilter {$Object -eq "We expected to find one of following Ancestors: ['master','additionalAncestor'], but found multiple possible Ancestors."}
      Mock Read-Host {return 1}

      Get-Ancestor $ExpectedAncestors | Should Be "additionalAncestor"

      Assert-MockCalled Write-Host 1
    }
  }

  Context "Push-To-Repos" {
    It "should push branch and given tag to remote" {
      #allow convenient synchronization of working directories between local and "remote" repo (no 'pull' required)
      #more infos: https://git-scm.com/docs/git-config#Documentation/git-config.txt-receivedenyCurrentBranch (default is 'refuse')
      git config receive.denyCurrentBranch updateInstead
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir

      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"
      git remote rename "origin" $TestRemoteName

      #add commits and tag
      git checkout -b "release/v1.2.3" --quiet
      Test-Add-Commit
      git commit --amend -m "Release with Tag"
      git tag -a "v1.2.3" -m "v1.2.3"

      #push changes and assert branch, commit and tag got created
      Push-To-Repos "release/v1.2.3" "v1.2.3" *>$NULL
      cd "$($TestBaseDir)\$($TestDirName)"

      Get-Branch-Exists "release/v1.2.3" | Should Be $TRUE
      Get-Tag-Exists "v1.2.3" | Should Be $TRUE
      git show --no-patch "release/v1.2.3" --format="%s" | Should Be "Release with Tag"
    }

    It "should push branch and no tags other than the one specified" {
      #allow convenient synchronization of working directories between local and "remote" repo (no 'pull' required)
      #more infos: https://git-scm.com/docs/git-config#Documentation/git-config.txt-receivedenyCurrentBranch (default is 'refuse')
      git config receive.denyCurrentBranch updateInstead
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir

      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"
      git remote rename "origin" $TestRemoteName

      #add commits and tags
      git checkout -b "release/v1.2.3" --quiet
      Test-Add-Commit
      git tag -a "v1.2.3-rc" -m "v1.2.3-rc"

      Test-Add-Commit
      git commit --amend -m "Actual Release with correct Tag"
      git tag -a "v1.2.3" -m "v1.2.3"

      #push changes and assert branch, commit and correct tag got created
      Push-To-Repos "release/v1.2.3" "v1.2.3" *>$NULL
      cd "$($TestBaseDir)\$($TestDirName)"

      Get-Branch-Exists "release/v1.2.3" | Should Be $TRUE
      Get-Tag-Exists "v1.2.3-rc" | Should Be $FALSE
      Get-Tag-Exists "v1.2.3" | Should Be $TRUE
      git show --no-patch "release/v1.2.3" --format="%s" | Should Be "Actual Release with correct Tag"
    }

    It "should push branch and no tag to remote if unspecified" {
      #allow convenient synchronization of working directories between local and "remote" repo (no 'pull' required)
      #more infos: https://git-scm.com/docs/git-config#Documentation/git-config.txt-receivedenyCurrentBranch (default is 'refuse')
      git config receive.denyCurrentBranch updateInstead
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir

      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"
      git remote rename "origin" $TestRemoteName

      #add commits and tag
      git checkout -b "release/v1.2.3" --quiet
      Test-Add-Commit
      git commit --amend -m "Release with Tag"
      git tag -a "v1.2.3" -m "v1.2.3"

      #push changes and assert branch, commit and no tag got created
      Push-To-Repos "release/v1.2.3" *>$NULL
      cd "$($TestBaseDir)\$($TestDirName)"

      Get-Branch-Exists "release/v1.2.3" | Should Be $TRUE
      Get-Tag-Exists "v1.2.3" | Should Be $FALSE
      git show --no-patch "release/v1.2.3" --format="%s" | Should Be "Release with Tag"
    }

    It "should throw if given tag does not exist" {
      #allow convenient synchronization of working directories between local and "remote" repo (no 'pull' required)
      #more infos: https://git-scm.com/docs/git-config#Documentation/git-config.txt-receivedenyCurrentBranch (default is 'refuse')
      git config receive.denyCurrentBranch updateInstead
      Test-Create-And-Add-Remote $TestBaseDir $TestDirName $PseudoRemoteTestDir

      cd "$($TestBaseDir)\$($PseudoRemoteTestDir)"
      git remote rename "origin" $TestRemoteName

      #add commits and tags
      git checkout -b "release/v1.2.3" --quiet

      #try to push changes and assert exception is thrown
      Test-Add-Commit
      git commit --amend -m "Release with Tag"
      git tag -a "v1.2.3" -m "v1.2.3"

      { Push-To-Repos "release/v1.2.3" "v1.2.4" *>$NULL } | Should Throw "Tag with name 'v1.2.4' does not exist, abort pushing branch and tag."
    }
  }
}