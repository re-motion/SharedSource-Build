using System;
using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

[CheckBuildProjectConfigurations]
internal partial class Build : NukeBuild
{
  private const string c_companyNamePropertyKey = "CompanyName";
  private const string c_productNamePropertyKey = "ProductName";
  private const string c_assemblyOriginatorKeyFilePropertyKey = "AssemblyOriginatorKeyFile";
  private const string c_companyUrlPropertyKey = "CompanyUrl";

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
  private IReadOnlyCollection<TestProjectMetadata> IntegrationTestProjectFiles { get; set; } = Array.Empty<TestProjectMetadata>();
  private IReadOnlyCollection<TestProjectMetadata> TestProjectFiles { get; set; } = Array.Empty<TestProjectMetadata>();

  [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
  private string[] Configuration { get; set; } = { "Debug", "Release" };

  [Parameter("Skip generation of nuget package with debug symbols - true / false")]
  private bool SkipNuGet { get; set; }

  [Parameter("Skip generation of nuget package with symbol server support - true / false")]
  private bool SkipNuGetOrg { get; set; }

  [Parameter("Skip compiling and running of tests - true / false")]
  private bool SkipTests { get; set; }

  private Target CleanFolders => _ => _
      .Before(ImportDefinitions)
      .Executes(() =>
      {
        FileSystemTasks.DeleteDirectory(DirectoryHelper.OutputDirectory);
        FileSystemTasks.DeleteDirectory(DirectoryHelper.LogDirectory);
        FileSystemTasks.DeleteDirectory(DirectoryHelper.TempDirectory);
      });

  public static int Main () => Execute<Build>(x => x.GenerateNuGetPackagesWithDebugSymbols);
}