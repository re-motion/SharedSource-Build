using System;
using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;

[CheckBuildProjectConfigurations]
internal partial class Build : NukeBuild
{
  [GitRepository]
  private readonly GitRepository GitRepository = null!;

#pragma warning disable CS0414
  [Solution]
  private readonly Solution Solution = null!;
#pragma warning disable CS0414

  private DirectoryHelper DirectoryHelper { get; } = new(RootDirectory);
  private AssemblyMetadata AssemblyMetadata { get; set; } = null!;
  private VersionHelper VersionHelper { get; set; } = null!;

  private IReadOnlyCollection<ProjectMetadata> ReleaseProjectFiles { get; set; } = Array.Empty<ProjectMetadata>();
  private IReadOnlyCollection<TestProjectMetadata> UnitTestProjectFiles { get; set; } = Array.Empty<TestProjectMetadata>();

  [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
  private string[] Configuration { get; set; } = { "Debug", "Release" };

  public static int Main () => Execute<Build>(x => x.CompileReleaseBuild);
}