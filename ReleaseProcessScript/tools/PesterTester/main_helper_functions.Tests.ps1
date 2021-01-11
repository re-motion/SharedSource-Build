. $PSScriptRoot"\..\Core\main_helper_functions.ps1"

Describe "main_helper_functions" {
    Context "Parse-Version-From-ReleaseBranch" {
        It "should split branch names correctly" {
            $Version = "1.2.3"
            Parse-Version-From-ReleaseBranch "release/v${Version}" | Should Be $Version
        }
    }
}