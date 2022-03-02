using System;
using System.Collections.Generic;

namespace ReleaseProcessScript.New;

public interface IGitClient
{
  IReadOnlyCollection<string> GetBranches ();

  string GetCurrentBranchName ();

  string GetFirstAncestor ();

  IReadOnlyCollection<string> GetAncestors (params string[] expectedAncestor);

  bool BranchExists (string branchName);

  bool RemoteBranchExists (string remoteName, string branchName);

  bool TagExists (string tagName);

  bool IsBranchMerged (string branchName, string possiblyMergedBranchName);

  void EnsureBranchUpToDate (string branchName);

  bool PushBranch (string branchName);

  bool PushTag (string tag);

  string? GetRemoteOfBranch (string branchName);
}