using System;
using Nuke.Common;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

public partial class Build : NukeBuild
{
  private const string c_packageVersionPropertyKey = "PackageVersion";

  private Target CompileReleaseBuild => _ => _
      .DependsOn(ImportDefinitions, RestoreReleaseBuild)
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(CompileProject);
      });

  private Target CompileTestBuild => _ => _
      .DependsOn(ImportDefinitions, RestoreTestBuild)
      .OnlyWhenStatic(() => !SkipTests)
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
              .SetTargetPath(projectFile.ProjectPath)
              .SetTargets(targets)
              .SetConfiguration(config)
              .SetAssemblyVersion(VersionHelper.AssemblyVersion)
              .SetFileVersion(VersionHelper.AssemblyFileVersion)
              .SetInformationalVersion(VersionHelper.GetAssemblyInformationalVersion(config, AdditionalBuildMetadata))
              .SetCopyright(AssemblyMetadata.Copyright)
              .SetProperty(c_packageVersionPropertyKey, VersionHelper.AssemblyNuGetVersion)
              .SetProperty(c_companyNamePropertyKey, AssemblyMetadata.CompanyName)
              .SetProperty(c_companyUrlPropertyKey, AssemblyMetadata.CompanyUrl)
              .SetProperty(c_productNamePropertyKey, AssemblyMetadata.ProductName)
              .SetProperty(c_assemblyOriginatorKeyFilePropertyKey, DirectoryHelper.SolutionKeyFile)
              .SetPackageProjectUrl(GitRepository.HttpsUrl)
              .SetToolsVersion(projectFile.ToolsVersion)
          );
        });
  }

  private void RestoreProject (ProjectMetadata projectFile)
  {
    MSBuild(s => s
        .SetTargetPath(projectFile.ProjectPath)
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