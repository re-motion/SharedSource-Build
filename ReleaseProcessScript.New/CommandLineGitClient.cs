using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ReleaseProcessScript.New;

public class CommandLineGitClient : IGitClient
{
  private class CommandLineResult
  {
    public bool Success { get; }
    public string Output { get; }

    public CommandLineResult (bool success, string output)
    {
      Success = success;
      Output = output;
    }
  }

  private readonly string _repositoryPath;

  public CommandLineGitClient (string repositoryPath)
  {
    _repositoryPath = repositoryPath;
  }

  private static CommandLineResult ExecuteGitCommandWithOutput (string arguments)
  {
    var psi = new ProcessStartInfo("git", arguments);
    psi.UseShellExecute = false;
    psi.RedirectStandardOutput = true;
    psi.RedirectStandardError = true;

    using var command = Process.Start(psi);
    command.WaitForExit();

    var output = command.StandardOutput.ReadToEnd();

    return new CommandLineResult(command.ExitCode == 0, output);
  }

  private static void ExecuteGitCommand (string arguments)
  {
    using var command = Process.Start("git", arguments);
    command.WaitForExit();
  }

  public bool PushTag (string tagName)
  {
    if (string.IsNullOrEmpty(tagName))
      throw new ArgumentException("Input tag was empty, abort pushing tag.", nameof(tagName));

    if (!TagExists(tagName))
      throw new InvalidOperationException($"Tag with name {tagName} does not exist, abort pushing tag.");

    var output = ExecuteGitCommandWithOutput($"push origin {tagName}");
    return output.Success;
  }

  public bool BranchExists (string branchName)
  {
    var output = ExecuteGitCommandWithOutput($"show-ref --verify refs/heads/{branchName}");

    return output.Output.Length > 0;
  }

  public bool RemoteBranchExists (string remoteName, string branchName)
  {
    var output = ExecuteGitCommandWithOutput($"ls-remote --heads {remoteName} {branchName}");

    return output.Output.Length > 0;
  }

  public bool TagExists (string tagName)
  {
    var output = ExecuteGitCommandWithOutput($"show-ref --verify refs/tags/{tagName}");

    return output.Output.Length > 0;
  }

  public bool IsBranchMerged (string branchName, string possiblyMergedBranchName)
  {
    ExecuteGitCommandWithOutput($"checkout {branchName}");
    var mergedBranches = ExecuteGitCommandWithOutput("branch --merged");

    return mergedBranches.Output.Contains(possiblyMergedBranchName);
  }

  public void EnsureBranchUpToDate (string branchName)
  {
    ExecuteGitCommand($"checkout {branchName}");

    var remoteName = "origin";

    if (!RemoteBranchExists(remoteName, branchName))
      throw new InvalidOperationException($"Remote branch of {branchName} does not exist");

    ExecuteGitCommand($"fetch {remoteName} {branchName}");

    var local = ExecuteGitCommandWithOutput($"rev-parse {branchName}").Output;
    var remote = ExecuteGitCommandWithOutput($"rev-parse {remoteName}/{branchName}").Output;
    var basis = ExecuteGitCommandWithOutput($"merge-base {branchName} {remoteName}/{branchName}").Output;

    if (local.Equals(remote))
    {
      //Up-To-Date. OK
    }
    else if (local.Equals(basis))
    {
      throw new InvalidOperationException($"Need to pull, local {branchName} branch is behind on repository {remoteName}");
    }
    else if (remote.Equals(basis))
    {
      //Need to push, remote branch is behind. Ok
    }
    else
    {
      throw new InvalidOperationException($"{branchName} diverged, need to rebase at repository {remoteName}");
    }
    
  }

  public bool PushBranch (string branchName)
  {
    ExecuteGitCommand($"checkout {branchName}");

    var remoteName = "origin";

    if (!string.IsNullOrEmpty(remoteName))
    {
      var upstream = "";
      var remoteNameOfBranch = GetRemoteOfBranch(branchName);

      if (string.IsNullOrEmpty(remoteNameOfBranch))
        if (remoteNameOfBranch.Equals(remoteName))
          upstream = "-u";

      return ExecuteGitCommandWithOutput($"push {upstream} {remoteName} {branchName}").Success;
    }

    return false;
  }

  public string? GetRemoteOfBranch (string branchName)
  {
    var output = ExecuteGitCommandWithOutput("status -sb");
    if (!output.Success)
      return null;

    return output.Output.Trim('#').Trim();
  }

  public IReadOnlyCollection<string> GetBranches ()
  {
    var branch = ExecuteGitCommandWithOutput("branch");
    return branch.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim('*', ' ')).ToArray();
  }

  public string GetCurrentBranchName ()
  {
    var name = ExecuteGitCommandWithOutput("symbolic-ref --short HEAD");
    if (!name.Success)
      throw new InvalidOperationException("No current branch defined.");

    return name.Output.Trim('*', ' ').Replace("\n", string.Empty);
  }

  public string GetFirstAncestor ()
  {
    var branchName = GetCurrentBranchName();

    var allAncestorsString = ExecuteGitCommandWithOutput("show-branch");

    if (!allAncestorsString.Success)
      throw new InvalidOperationException("Did not find first Ancestor");

    var allAncestors = allAncestorsString.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries);
    
    var selectAncestor = allAncestors
        .Where(ancestor => ancestor.Contains('*'))
        .First(ancestor => !ancestor.Contains(branchName));

    return selectAncestor;
  }

  public IReadOnlyCollection<string> GetAncestors (params string[] expectedAncestor)
  {
    var branchName = GetCurrentBranchName();

    var allAncestorsString = ExecuteGitCommandWithOutput("show-branch");
    var allAncestors = allAncestorsString.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());

    var selectAncestors = allAncestors
        .Where(ancestor => ancestor.Contains('!'))
        .Where(ancestor => !ancestor.Contains(branchName))
        .ToArray();

    for (var i = 0; i < selectAncestors.Length; i++)
    {
      selectAncestors[i] = selectAncestors[i][(selectAncestors[i].IndexOf('[') + 1)..];
      selectAncestors[i] = selectAncestors[i][..selectAncestors[i].IndexOf(']')];
    }

    var foundAncestors = new List<string>();

    foreach (var ancestor in selectAncestors)
    foreach (var expected in expectedAncestor)
      if (expected.Contains(ancestor) || ancestor.Contains(expected))
        foundAncestors.Add(ancestor);

    return foundAncestors;
  }
}