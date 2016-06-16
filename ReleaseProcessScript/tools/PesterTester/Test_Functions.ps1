function Test-Create-Repository ($DirName)
{
    New-Item $DirName -ItemType directory
    cd $DirName
    git init
    Test-Add-Commit
    cd ".."
}

function Test-Add-Commit ($Amend)
{
    $RandomValue = Get-Random
    
    New-Item $RandomValue -ItemType File -Value $RandomValue
    git add .
    git commit -m $RandomValue $Amend
}

function Test-Get-First-Parent-CommitHash-Short ($Branchname)
{
    $FirstParentHash = git rev-list --first-parent $Branchname
    $FirstParentShortHash = $FirstParentHash.Substring(0, 7)
    
    return $FirstParentShortHash[0]
}

function Test-Create-And-Add-Remote ($TestBaseDir, $TestDirName, $PseudoRemoteTestDir)
{
    cd $TestBaseDir

    New-Item $PseudoRemoteTestDir -ItemType directory
    cd "$($TestBaseDir)\\$($PseudoRemoteTestDir)"
    $FileName = "file:///$($TestBaseDir)/$($TestDirName)"
    git clone $FileName "." 2>&1 > $NULL

    cd $TestBaseDir"\"$TestDirName
}

function Test-Mock-All-Jira-Functions()
{
    Mock Jira-Create-Version { return $TRUE }
    Mock Jira-Get-Current-Version { return "1.2.3" }
    Mock Jira-Release-Version { return $TRUE }
    Mock Jira-Release-Version-And-Squash-Unreleased { return $TRUE }
    Mock Jira-Check-Credentials { return $TRUE }
}