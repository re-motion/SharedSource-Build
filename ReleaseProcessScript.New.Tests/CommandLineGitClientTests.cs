using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace ReleaseProcessScript.New.Tests;

[TestFixture]
internal class CommandLineGitClientTests
{
  [SetUp]
  public void Setup ()
  {
    _previousWorkingDirectory = Environment.CurrentDirectory;
    var temp = Path.GetTempPath();

    var guid = Guid.NewGuid();
    var path = Path.Combine(temp, guid.ToString());
    _remotePath = Directory.CreateDirectory(path).FullName;

    Environment.CurrentDirectory = _remotePath;

    ExecuteGitCommand("--bare init");

    guid = Guid.NewGuid();

    path = Path.Combine(temp, guid.ToString());
    _repositoryPath = Directory.CreateDirectory(path).FullName;

    Environment.CurrentDirectory = _repositoryPath;

    ExecuteGitCommand("init");
    ExecuteGitCommand($"remote add {c_remoteName} {_remotePath}");
    ExecuteGitCommand("commit -m \"Initial Commit\" --allow-empty");
    ExecuteGitCommand($"push {c_remoteName} --all");
  }

  [TearDown]
  public void TearDown ()
  {
    Environment.CurrentDirectory = _previousWorkingDirectory;
    DeleteDirectory(_repositoryPath);
    DeleteDirectory(_remotePath);
  }

  private const string c_remoteName = "origin";
  private string _previousWorkingDirectory;
  private string _repositoryPath;
  private string _remotePath;

  [Test]
  public void GetBranches_WithTwoBranches_ReturnsBranches ()
  {
    ExecuteGitCommand("branch develop");
    var client = new CommandLineGitClient(_repositoryPath);

    var branches = client.GetBranches();

    Assert.That(branches, Does.Contain("master"));
    Assert.That(branches, Does.Contain("develop"));
  }

  [Test]
  public void GetCurrentBranchName_WithOneBranch_ReturnsBranchName ()
  {
    var client = new CommandLineGitClient(_repositoryPath);

    var branchName = client.GetCurrentBranchName();

    Assert.That(branchName, Is.EqualTo("master"));
  }

  [Test]
  public void GetCurrentBranchName_WithTwoBranches_ReturnsBranchName ()
  {
    var client = new CommandLineGitClient(_repositoryPath);
    ExecuteGitCommand("branch develop");

    var branchName = client.GetCurrentBranchName();

    Assert.That(branchName, Is.EqualTo("master"));
  }

  [Test]
  public void BranchExists_WithOneBranch_ReturnsTrue ()
  {
    var client = new CommandLineGitClient(_repositoryPath);

    var outcome = client.BranchExists("master");

    Assert.That(outcome, Is.True);
  }

  [Test]
  public void RemoteBranchExists_BranchExists_ReturnsTrue ()
  {
    var client = new CommandLineGitClient(_repositoryPath);
    ExecuteGitCommand("checkout -b newBranch");
    AddCommit();
    ExecuteGitCommand("push -u origin newBranch");

    var exists = client.RemoteBranchExists("origin", "newBranch");

    Assert.That(exists, Is.True);
  }

  [Test]
  public void RemoteBranchExists_BranchNotExists_ReturnsFalse ()
  {
    var client = new CommandLineGitClient(_repositoryPath);
    ExecuteGitCommand("checkout -b newBranch");
    AddCommit();
    ExecuteGitCommand("push -u origin newBranch");

    var exists = client.RemoteBranchExists("origin", "oldBranch");

    Assert.That(exists, Is.False);
  }

  [Test]
  public void EnsureBranchUpToDate_BranchBehind_ShouldThrowException ()
  {
    var client = new CommandLineGitClient(_repositoryPath);
    var old = ExecuteGitCommandWithOutput("log --pretty=format:\"%h\"");
    ExecuteGitCommand("checkout master");
    AddCommit();
    ExecuteGitCommandWithOutput("checkout master");
    AddCommit();
    ExecuteGitCommand("checkout master");
    ExecuteGitCommand($"push -u {c_remoteName} master");
    ExecuteGitCommand($"reset --hard {old}");

    Assert.That(
        () => client.EnsureBranchUpToDate("master"),
        Throws.InstanceOf<InvalidOperationException>()
            .With.Message.EqualTo("Need to pull, local master branch is behind on repository origin"));
  }

  [Test]
  public void TagExists_WithOneTag_ReturnsTrue ()
  {
    ReleaseVersion("v1.0.0");
    var client = new CommandLineGitClient(_repositoryPath);

    var outcome = client.TagExists("v1.0.0");

    Assert.That(outcome, Is.True);
  }

  [Test]
  public void TagExists_WithoutTag_ReturnsFalse ()
  {
    var client = new CommandLineGitClient(_repositoryPath);

    var outcome = client.TagExists("v1.0.0");

    Assert.That(outcome, Is.False);
  }

  [Test]
  public void GetAncestors_DevelopBehindReleaseBranch_ReturnsOne ()
  {
    AddCommit();
    ExecuteGitCommand("checkout -b develop");
    AddCommit();
    ExecuteGitCommand("checkout -b release/v1.0.1");
    AddCommit();
    var client = new CommandLineGitClient(_repositoryPath);

    var ancestor = client.GetAncestors("develop");

    Assert.That(ancestor, Does.Contain("develop"));
    Assert.That(ancestor, Does.Not.Contain("master"));
    Assert.That(ancestor, Does.Not.Contain("release/v1.0.1"));
  }

  [Test]
  public void GetAncestors_ReleaseBehindDevelopBranch_ReturnsOne ()
  {
    AddCommit();
    ExecuteGitCommand("checkout -b develop");
    AddCommit();
    ExecuteGitCommand("checkout -b release/v1.0.1");
    AddCommit();
    ExecuteGitCommand("checkout develop");
    AddCommit();
    ExecuteGitCommand("checkout release/v1.0.1");
    ExecuteGitCommand("checkout master");

    var client = new CommandLineGitClient(_repositoryPath);

    var ancestor = client.GetAncestors("develop", "release");

    Assert.That(ancestor, Does.Contain("develop"));
    Assert.That(ancestor, Does.Contain("release/v1.0.1"));
    Assert.That(ancestor, Does.Not.Contain("master"));
  }

  [Test]
  public void GetAncestors_ReleaseOnSameCommitAsDevelop_ReturnsTwo ()
  {
    AddCommit();
    ExecuteGitCommand("checkout -b develop");
    ExecuteGitCommand("checkout -b developer");
    ExecuteGitCommand("checkout -b release/v1.0.1");
    var client = new CommandLineGitClient(_repositoryPath);

    var ancestor = client.GetAncestors("develop");

    Assert.That(ancestor, Does.Contain("develop"));
    Assert.That(ancestor, Does.Contain("developer"));
    Assert.That(ancestor, Does.Not.Contain("release/v1.0.1"));
  }

  [Test]
  public void GetAncestor_SeveralBranchesFromMaster_ReturnsOnlyOne ()
  {
    AddCommit();
    ExecuteGitCommand("checkout -b develop");
    AddCommit();
    ExecuteGitCommand("checkout master");
    ExecuteGitCommand("checkout -b developer");
    AddCommit();
    ExecuteGitCommand("checkout -b release/v1.0.1");
    ExecuteGitCommand("checkout -b developeria");
    AddCommit();
    ExecuteGitCommand("checkout -b release/v1.0.2");
    var client = new CommandLineGitClient(_repositoryPath);

    var ancestor = client.GetAncestors("develop");

    Assert.That(ancestor, Does.Contain("developer"));
    Assert.That(ancestor, Does.Contain("developeria"));
  }

  private static void ExecuteGitCommand (string argument)
  {
    using var command = Process.Start("git", argument);
    command.WaitForExit();
  }

  private static string ExecuteGitCommandWithOutput (string argument)
  {
    var psi = new ProcessStartInfo("git", argument);
    psi.UseShellExecute = false;
    psi.RedirectStandardOutput = true;
    psi.RedirectStandardError = true;

    using var command = Process.Start(psi);
    command.WaitForExit();

    var output = command.StandardOutput.ReadToEnd();

    return output;
  }

  private static void DeleteDirectory (string target_dir)
  {
    var files = Directory.GetFiles(target_dir);
    var dirs = Directory.GetDirectories(target_dir);

    foreach (var file in files)
    {
      File.SetAttributes(file, FileAttributes.Normal);
      File.Delete(file);
    }

    foreach (var dir in dirs)
      DeleteDirectory(dir);

    Directory.Delete(target_dir, false);
  }

  private void AddCommit (string s = "")
  {
    var random = new Random().Next();
    ExecuteGitCommand($"commit --allow-empty -m {random}");
  }

  private void ReleaseVersion (string version)
  {
    var releaseBranchName = $"release/{version}";
    ExecuteGitCommand($"checkout -b {releaseBranchName}");
    AddCommit();
    ExecuteGitCommand($"commit --amend -m \"Release Version {version}\"");
    ExecuteGitCommand($"tag -a {version} -m {version}");
  }
}