using System;
using Nuke.Common;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

public partial class Build : NukeBuild
{
  private const string c_packageVersionPropertyKey = "PackageVersion";

  private Target CompileReleaseBuild => _ => _
      .DependsOn(ReadConfiguration, RestoreReleaseBuild)
      .Description("Compile release projects")
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(CompileProject);
      });

  private Target CompileTestBuild => _ => _
      .DependsOn(ReadConfiguration, RestoreTestBuild)
      .Description("Compile test projects")
      .OnlyWhenStatic(() => !SkipTests)
      .Executes(() =>
      {
        TestProjectFiles.ForEach(CompileProject);
      });

  public Target Restore => _ => _
      .Description("Restores all projects")
      .DependsOn(RestoreReleaseBuild, RestoreTestBuild);

  private Target RestoreReleaseBuild => _ => _
      .DependsOn(ReadConfiguration)
      .Unlisted()
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(RestoreProject);
      });

  private Target RestoreTestBuild => _ => _
      .DependsOn(ReadConfiguration)
      .Unlisted()
      .Executes(() =>
      {
        TestProjectFiles.ForEach(RestoreProject);
      });

  private void CompileProject (ProjectMetadata projectFile)
  {
    var targets = GetCompileTargets(projectFile);
    Configuration.ForEach(
        config =>
        {
          MSBuild(s => s
              .SetTargetPath(projectFile.ProjectPath)
              .SetTargets(targets)
              .SetConfiguration(config)
              .SetAssemblyVersion(SemanticVersion.AssemblyVersion)
              .SetFileVersion(SemanticVersion.AssemblyFileVersion)
              .SetInformationalVersion(SemanticVersion.GetAssemblyInformationalVersion(config, AdditionalBuildMetadata))
              .SetCopyright(AssemblyMetadata.Copyright)
              .SetProperty(c_packageVersionPropertyKey, SemanticVersion.AssemblyNuGetVersion)
              .SetProperty(c_companyNamePropertyKey, AssemblyMetadata.CompanyName)
              .SetProperty(c_companyUrlPropertyKey, AssemblyMetadata.CompanyUrl)
              .SetProperty(c_productNamePropertyKey, AssemblyMetadata.ProductName)
              .SetProperty(c_assemblyOriginatorKeyFilePropertyKey, Directories.SolutionKeyFile)
              .SetPackageProjectUrl(GitRepository.HttpsUrl)
              .SetToolsVersion(projectFile.ToolsVersion)
          );
        });
  }

  private void RestoreProject (ProjectMetadata projectFile)
  {
    MSBuild(s => s
        .SetTargetPath(projectFile.ProjectPath)
        .SetTargets(MSBuildTargets.Restore)
    );
  }

  private string GetCompileTargets (ProjectMetadata project)
  {
    if (project.IsMultiTargetFramework)
      return MSBuildTargets.DispatchToInnerBuilds;
    return MSBuildTargets.Build;
  }
}