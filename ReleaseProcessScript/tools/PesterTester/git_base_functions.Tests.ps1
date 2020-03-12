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
  }
}