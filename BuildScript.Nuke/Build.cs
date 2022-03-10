using System;
using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;

[CheckBuildProjectConfigurations]
public partial class Build : NukeBuild
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
  private SemanticVersion SemanticVersion { get; set; } = null!;

  private IReadOnlyCollection<ProjectMetadata> ReleaseProjectFiles { get; set; } = Array.Empty<ProjectMetadata>();
  private IReadOnlyCollection<TestProjectMetadata> UnitTestProjectFiles { get; set; } = Array.Empty<TestProjectMetadata>();
  private IReadOnlyCollection<TestProjectMetadata> IntegrationTestProjectFiles { get; set; } = Array.Empty<TestProjectMetadata>();
  private IReadOnlyCollection<TestProjectMetadata> TestProjectFiles { get; set; } = Array.Empty<TestProjectMetadata>();

  [Parameter("Skip generation of nuget package with debug symbols - true / false")]
  private bool SkipNuGet { get; set; }

  [Parameter("Skip generation of nuget package with symbol server support - true / false")]
  private bool SkipNuGetOrg { get; set; }

  [Parameter("Skip compiling and running of tests - true / false")]
  private bool SkipTests { get; set; }

  [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
  private string[] Configuration { get; set; } = { "Debug", "Release" };

  [Parameter("Browser available for the build to use for test running")]
  private string[] Browsers { get; set; } = { "NoBrowser" };

  [Parameter("Target runtimes available for the build to use for test running")]
  private string[] TargetRuntimes { get; set; } = { "NET48", "NET45", "NET50", "NET461" };

  [Parameter("Execution runtimes available for the build to use for test running")]
  private string[] ExecutionRuntimes { get; set; } = { "LocalMachine" };

  [Parameter("Database runtimes available for the build to use for test running")]
  private string[] DatabaseSystems { get; set; } = { "NoDB" };

  [Parameter("Platforms available for the build to use for test running")]
  private string[] Platforms { get; set; } = { "x86", "x64" };

  [Parameter("Test Categories to exclude for test running")]
  private string[] TestCategoriesToExclude { get; set; } = { };

  [Parameter("Test Categories to include for test running")]
  private string[] TestCategoriesToInclude { get; set; } = { };

  public static int Main () => Execute<Build>(x => x.RunTests);
}