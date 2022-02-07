using System;
using Nuke.Common;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

internal partial class Build : NukeBuild
{
  private Target CompileReleaseBuild => _ => _
      .DependsOn(ImportDefinitions, RestoreReleaseBuild)
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(CompileProject);
      });

  private Target CompileTestBuild => _ => _
      .DependsOn(ImportDefinitions, RestoreTestBuild)
      .Executes(() =>
      {
        TestProjectFiles.ForEach(CompileProject);
      });

  private Target RestoreReleaseBuild => _ => _
      .DependsOn(ImportDefinitions)
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(RestoreProject);
      });

  private Target RestoreTestBuild => _ => _
      .DependsOn(ImportDefinitions)
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
              .SetTargetPath(projectFile.Path)
              .SetTargets(targets)
              .SetConfiguration(config)
              .SetAssemblyVersion(VersionHelper.AssemblyVersion)
              .SetFileVersion(VersionHelper.AssemblyFileVersion)
              .SetInformationalVersion(VersionHelper.GetAssemblyInformationVersion(config, AdditionalBuildMetadata))
              .SetCopyright(AssemblyMetadata.Copyright)
              .SetProperty("PackageVersion", VersionHelper.AssemblyNuGetVersion)
              .SetProperty("CompanyName", AssemblyMetadata.CompanyName)
              .SetProperty("CompanyUrl", AssemblyMetadata.CompanyUrl)
              .SetProperty("ProductName", AssemblyMetadata.ProductName)
              .SetProperty("AssemblyOriginatorKeyFile", DirectoryHelper.SolutionKeyFile)
              .SetPackageProjectUrl(GitRepository.HttpsUrl)
              .SetToolsVersion(projectFile.ToolsVersion)
          );
        });
  }

  private void RestoreProject (ProjectMetadata projectFile)
  {
    MSBuild(s => s
        .SetTargetPath(projectFile.Path)
        .SetTargets("Restore")
    );
  }

  private string GetCompileTargets (ProjectMetadata project)
  {
    if (project.IsMultiTargetFramework)
      return "DispatchToInnerBuilds";
    return "Build";
  }
}