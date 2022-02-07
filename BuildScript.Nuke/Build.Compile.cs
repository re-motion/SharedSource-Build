using System;
using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

internal partial class Build : NukeBuild
{
  public Target CompileReleaseBuild => _ => _
      .DependsOn(ImportDefinitions)
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(projectFile =>
        {
          var targets = new List<string> { "Build" };
          if (projectFile.IsMultiTargetFramework)
            targets.Add("DispatchToInnerBuilds");

          Configuration.ForEach(config =>
          {
            MSBuild(s => s
                .SetTargetPath(projectFile.Path)
                .SetTargets(targets)
                .SetConfiguration(config)
                .SetAssemblyVersion(VersionHelper.AssemblyVersion)
                .SetFileVersion(VersionHelper.AssemblyFileVersion)
                .SetInformationalVersion(VersionHelper.GetAssemblyInformationVersion(config, AdditionalBuildMetadata))
                .SetCopyright(AssemblyMetadata.Copyright)
                .SetProperty("CompanyName", AssemblyMetadata.CompanyName)
                .SetProperty("CompanyUrl", AssemblyMetadata.CompanyUrl)
                .SetProperty("ProductName", AssemblyMetadata.ProductName)
                .SetProperty("AssemblyOriginatorKeyFile", DirectoryHelper.SolutionKeyFile)
                .SetProperty("OutputDirectory", DirectoryHelper.OutputDirectory)
                .SetProperty("TempDirectory", DirectoryHelper.TempDirectory)
                .SetProperty("LogDirectory", DirectoryHelper.LogDirectory)
                .SetPackageProjectUrl(GitRepository.HttpsUrl)
                .SetToolsVersion(projectFile.ToolsVersion)
            );
          });
        });
      });
}