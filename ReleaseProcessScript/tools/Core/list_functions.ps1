function List-Contains-Only-Develop ($BranchesOnHead)
{
  return $BranchesOnHead -contains "develop" -and $BranchesOnHead -cnotcontains "master" -and ($BranchesOnHead | Where-Object { $_ -ne $NULL } | Where-Object { $_.StartsWith("support/v") }).Count -eq 0
}

function List-Contains-Only-Master ($BranchesOnHead)
{
  return $BranchesOnHead -contains "master" -and $BranchesOnHead -cnotcontains "develop" -and ($BranchesOnHead | Where-Object { $_ -ne $NULL } | Where-Object { $_.StartsWith("support/v") }).Count -eq 0
}

function List-Contains-Only-One-Support ($BranchesOnHead)
{
  return $BranchesOnHead -cnotcontains "master" -and $BranchesOnHead -cnotcontains "develop" -and ($BranchesOnHead | Where-Object { $_ -ne $NULL } | Where-Object { $_.StartsWith("support/v") }).Count -eq 1
}