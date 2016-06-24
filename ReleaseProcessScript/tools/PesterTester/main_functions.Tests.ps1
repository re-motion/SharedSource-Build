. $PSScriptRoot"\..\Core\main_functions.ps1"
. $PSScriptRoot"\Test_Functions.ps1"
. $PSScriptRoot"\..\Core\config_functions.ps1"
. $PSScriptRoot"\..\Core\jira_functions.ps1"
. $PSScriptRoot"\..\Core\main_helper_functions.ps1"
. $PSScriptRoot"\..\Core\read_functions.ps1"

#There was an Issue that $PSScriptRoot was null in BeforeEach/AfterEach, so we have to cache it here
$ScriptRoot = $PSScriptRoot

$TestBaseDir = "C:\temp"
$TestDirName = "GitUnitTestDir"
$PseudoRemoteTestDir = "RemoteTestDir"

Describe "main_functions" {

    BeforeEach {
      #Run once to save Config file in global Memory
      Load-Config-File

      Test-Create-Repository "$($TestBaseDir)\\$($TestDirName)"
      cd "$($TestBaseDir)\\$($TestDirName)"
      Test-Mock-All-Jira-Functions
    }

    AfterEach {
      cd $TestBaseDir
      Remove-Item -Recurse -Force $TestDirName
      Remove-Item -Recurse -Force $PseudoRemoteTestDir 2>&1 | Out-Null
      cd $ScriptRoot
    }

    Context "Create-Tag-And-Merge" {
        It "Create-Tag-And-Merge_ShouldWork" {
            $MasterCommitHash = git rev-parse --short HEAD

            git checkout -b develop --quiet
            Test-Add-Commit
            $DevelopCommitHash = git rev-parse --short HEAD

            git checkout -b "release/v1.0.0" --quiet
            Test-Add-Commit



            { Create-Tag-And-Merge } | Should Not Throw

            #Assert Tag got created
            Get-Tag-Exists "v1.0.0" | Should Be $TRUE

            #Assert develop und master got new commits
            git checkout master --quiet
            git rev-parse --short HEAD | Should Not Be $MasterCommitHash
            
            git checkout develop --quiet
            git rev-parse --short HEAD | Should Not Be $DevelopCommitHash
        }

        It "Create-Tag-And-Merge_NotOnReleaseBranch_ThrowsException" {
            { Create-Tag-And-Merge } | Should Throw "You have to be on 'release/' branch for this operation."
        }

        It "Create-Tag-And-Merge_MasterAndOrDevelopBranchMissing_ThrowsException" {
            git checkout -b "release/v1.0.0" --quiet

            #develop missing
            {Create-Tag-And-Merge } | Should Throw "'develop' does not exist. Please ensure its existence before proceeding."
            
            git checkout -b develop --quiet
            git branch -d master --quiet

            git checkout release/v1.0.0 --quiet

            #master missing
            {Create-Tag-And-Merge } | Should Throw "'master' does not exist. Please ensure its existence before proceeding." 
        }

        It "Create-Tag-And-Merge_TagAlreadyExists_ThrowsException" {
            git tag -a "v1.0.0" -m "v1.0.0" 2>&1 > $NULL
            git checkout -b develop --quiet

            git checkout -b "release/v1.0.0" --quiet
            Test-Add-Commit

            git checkout develop --quiet
            Test-Add-Commit
            git checkout release/v1.0.0 --quiet
           
            { Create-Tag-And-Merge } | Should Throw "There is already a commit tagged with 'v1.0.0'."
        }
    }
}