. $PSScriptRoot"\..\Core\list_functions.ps1"

Describe "list_functions" {
  Context "List-Contains-Only-Develop" {
    It "List-Contains-Only-Develop_True" {
      $BranchesOnHead = "develop", "notmaster", "notsupport"
      List-Contains-Only-Develop $BranchesOnHead | Should Be $TRUE
    }
      
    It "List-Contains-Only-Develop_WithMaster" {
      $BranchesOnHead = "develop", "master", "notsupport"
      List-Contains-Only-Develop $BranchesOnHead | Should Be $False
    }
      
    It "List-Contains-Only-Develop_WithSupport" {
      $BranchesOnHead = "develop", "notmaster", "support/v1.1"
      List-Contains-Only-Develop $BranchesOnHead | Should Be $False
    }
      
    It "List-Contains-Only-Develop_WithSupportAndMaster" {
      $BranchesOnHead = "develop", "master", "support/v1.1"
      List-Contains-Only-Develop $BranchesOnHead | Should Be $False
    }

    It "List-Contains-Only-Develop_WithSupportAndMasterNoDevelop" {
      $BranchesOnHead = "nodevelop", "master", "support/v1.1"
      List-Contains-Only-Develop $BranchesOnHead | Should Be $False
    }
  }

  Context "List-Contains-Only-Master" {
    It "List-Contains-Only-Master_True" {
      $BranchesOnHead = "master", "notdevelop", "notsupport"
      List-Contains-Only-Master $BranchesOnHead | Should Be $TRUE
    }
      
    It "List-Contains-Only-Master_WithDevelop" {
      $BranchesOnHead = "master", "develop", "notsupport"
      List-Contains-Only-Master $BranchesOnHead | Should Be $False
    }
      
    It "List-Contains-Only-Master_WithSupport" {
      $BranchesOnHead = "master", "notdevelop", "support/v1.1"
      List-Contains-Only-Master $BranchesOnHead | Should Be $False
    }
      
    It "List-Contains-Only-Master_WithSupportAndDevelop" {
      $BranchesOnHead = "master", "develop", "support/v1.1"
      List-Contains-Only-Master $BranchesOnHead | Should Be $False
    }

    It "List-Contains-Only-Master_WithSupportAndDevelopNoMaster" {
      $BranchesOnHead = "nomaster", "develop", "support/v1.1"
      List-Contains-Only-Master $BranchesOnHead | Should Be $False
    }
  }

  Context "List-Contains-Only-One-Support" {
    It "List-Contains-Only-One-Support_True" {
      $BranchesOnHead = "support/v1.1", "notdevelop", "notmaster"
      List-Contains-Only-One-Support $BranchesOnHead | Should Be $TRUE
    }
      
    It "List-Contains-Only-One-Support_WithDevelop" {
      $BranchesOnHead = "support/v1.1", "develop", "notmaster"
      List-Contains-Only-One-Support $BranchesOnHead | Should Be $False
    }
      
    It "List-Contains-Only-One-Support_WithMaster" {
      $BranchesOnHead = "support/v1.1", "notdevelop", "master"
      List-Contains-Only-One-Support $BranchesOnHead | Should Be $False
    }
      
    It "List-Contains-Only-One-Support_WithMasterAndDevelop" {
      $BranchesOnHead = "support/v1.1", "develop", "master"
      List-Contains-Only-One-Support $BranchesOnHead | Should Be $False
    }

    It "List-Contains-Only-One-Support_WithDevelopAndMasterNoSupport" {
      $BranchesOnHead = "support/v1.1", "develop", "master"
      List-Contains-Only-One-Support $BranchesOnHead | Should Be $False
    }

    It "List-Contains-Only-One-Support_WithMultipleSupport" {
      $BranchesOnHead = "support/v1.1", "support/v1.2", "develop", "master"
      List-Contains-Only-One-Support $BranchesOnHead | Should Be $False
    }
  }
}  
