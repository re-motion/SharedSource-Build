// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
//

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

namespace Remotion.BuildScript;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public partial class BaseBuild : NukeBuild
{
  [PublicAPI]
  public Target CompileReleaseBuild => _ => _
      .DependsOn(ReadConfiguration, RestoreReleaseBuild)
      .Description("Compile release projects")
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(CompileProject);
      });

  [PublicAPI]
  public Target CompileTestBuild => _ => _
      .DependsOn(ReadConfiguration, RestoreTestBuild)
      .Description("Compile test projects")
      .OnlyWhenStatic(() => !SkipTests)
      .Executes(() =>
      {
        TestProjectFiles.ForEach(CompileProject);
      });

  [PublicAPI]
  public Target Restore => _ => _
      .Description("Restores all projects")
      .DependsOn(RestoreReleaseBuild, RestoreTestBuild);

  protected Target RestoreReleaseBuild => _ => _
      .DependsOn(ReadConfiguration)
      .Unlisted()
      .Executes(() =>
      {
        ReleaseProjectFiles.ForEach(RestoreProject);
      });


  protected Target RestoreTestBuild => _ => _
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
              .SetProperty(MSBuildProperties.PackageVersion, SemanticVersion.AssemblyNuGetVersion)
              .SetProperty(MSBuildProperties.CompanyName, AssemblyMetadata.CompanyName)
              .SetProperty(MSBuildProperties.CompanyUrl, AssemblyMetadata.CompanyUrl)
              .SetProperty(MSBuildProperties.ProductName, AssemblyMetadata.ProductName)
              .SetProperty(MSBuildProperties.AssemblyOriginatorKeyFile, Directories.SolutionKeyFile)
              .When(GitRepository != null, s => s.SetPackageProjectUrl(GitRepository!.HttpsUrl))
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