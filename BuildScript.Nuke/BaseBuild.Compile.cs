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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

namespace Remotion.BuildScript;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public partial class BaseBuild : NukeBuild
{
  protected readonly Lazy<string> ToolPath;

  private IReadOnlyCollection<Project>? _sortedProjectList;

  [Parameter("MSBuild Path to exe")]
  protected string MsBuildPath { get; set; } = "";

  [Parameter("VisualStudio version")]
  protected VisualStudioVersion? VisualStudioVersion { get; set; }

  private IReadOnlyCollection<Project> SortedProjectList => _sortedProjectList ??= GetSortedProjects(Solution.Path).GetAwaiter().GetResult();

  [PublicAPI]
  public Target CompileReleaseBuild => _ => _
      .DependsOn(ReadConfiguration, RestoreReleaseBuild)
      .Description("Compile release projects")
      .Executes(() =>
      {
        CompileProjects(ReleaseProjectFiles);
      });

  [PublicAPI]
  public Target CompileTestBuild => _ => _
      .DependsOn(ReadConfiguration, RestoreTestBuild)
      .Description("Compile test projects")
      .OnlyWhenStatic(() => !SkipTests)
      .After(CompileReleaseBuild)
      .Executes(() =>
      {
        CompileProjects(TestProjectFiles);
      });

  [PublicAPI]
  public Target Restore => _ => _
      .Description("Restores all projects")
      .DependsOn(RestoreReleaseBuild, RestoreTestBuild);

  protected Target RestoreReleaseBuild => _ => _
      .DependsOn(ReadConfiguration, CleanFolders)
      .Unlisted()
      .Executes(() =>
      {
        RestoreProjects(ReleaseProjectFiles);
      });

  protected Target RestoreTestBuild => _ => _
      .DependsOn(ReadConfiguration, CleanFolders)
      .Unlisted()
      .Executes(() =>
      {
        RestoreProjects(TestProjectFiles);
      });

  private void CompileProjects (IReadOnlyCollection<ProjectMetadata> projectFiles)
  {
    var projectFilesDebugTargets = projectFiles
        .Where(file => file.Configuration == "Debug").ToList();
    var projectFilesReleaseTargets = projectFiles
        .Where(file => file.Configuration == "Release").ToList();
    if (projectFilesDebugTargets.Count > 0) CompileProject(projectFilesDebugTargets, "Debug");

    if (projectFilesReleaseTargets.Count > 0) CompileProject(projectFilesReleaseTargets, "Release");
  }

  private void CompileProject (IReadOnlyCollection<ProjectMetadata> projects, string configuration)
  {
    var sortedProjects = SortedProjectList;
    var filteredProjects = sortedProjects.Select(projectSort => projects.SingleOrDefault(project => projectSort.FilePath == project.ProjectPath)).WhereNotNull().Distinct();

    filteredProjects.ForEach(project =>
    {
      MSBuild(s => s
          .SetProcessToolPath(ToolPath.Value)
          .SetAssemblyVersion(SemanticVersion.AssemblyVersion)
          .SetFileVersion(SemanticVersion.AssemblyFileVersion)
          .SetCopyright(AssemblyMetadata.Copyright)
          .SetProperty(MSBuildProperties.PackageVersion, SemanticVersion.AssemblyNuGetVersion)
          .SetProperty(MSBuildProperties.CompanyName, AssemblyMetadata.CompanyName)
          .SetProperty(MSBuildProperties.CompanyUrl, AssemblyMetadata.CompanyUrl)
          .SetProperty(MSBuildProperties.ProductName, AssemblyMetadata.ProductName)
          .SetProperty(MSBuildProperties.AssemblyOriginatorKeyFile, Directories.SolutionKeyFile)
          .When(GitRepository != null, s => s.SetPackageProjectUrl(GitRepository!.HttpsUrl))
          .SetToolsVersion(project.ToolsVersion)
          .SetConfiguration(configuration)
          .SetInformationalVersion(
              SemanticVersion.GetAssemblyInformationalVersion(configuration, AdditionalBuildMetadata))
          .SetTargetPath(project.ProjectPath)
          .SetTargets(project.IsMultiTargetFramework ? MSBuildTargets.DispatchToInnerBuilds : MSBuildTargets.Build)
      );
    });
  }

  private string GetToolPath ()
  {
    var toolPath = MSBuildPath;
    var editions = new[] { "Enterprise", "Professional", "Community", "BuildTools", "Preview" };
    if (!string.IsNullOrEmpty(MsBuildPath))
      toolPath = MsBuildPath;
    else if (VisualStudioVersion != null)
      toolPath = editions
          .Select(
              edition => Path.Combine(
                  EnvironmentInfo.SpecialFolder(SpecialFolders.ProgramFilesX86)!,
                  $@"Microsoft Visual Studio\{VisualStudioVersion.VsVersion}\{edition}\MSBuild\{VisualStudioVersion.MsBuildVersion}\Bin\msbuild.exe"))
          .First(File.Exists);
    return toolPath;
  }

  private static async Task<IReadOnlyCollection<Project>> GetSortedProjects (string solutionPath)
  {
    var workspace = MSBuildWorkspace.Create();
    var solution = await workspace.OpenSolutionAsync(solutionPath);
    return solution
        .GetProjectDependencyGraph()
        .GetTopologicallySortedProjects()
        .Select(proj =>
        {
          if (solution.ContainsProject(proj))
            return solution.GetProject(proj);
          Assert.Fail($"Project {proj} cannot be found in the solution");
          return null;
        })
        .ToList()!;
  }

  private void RestoreProjects (IReadOnlyCollection<ProjectMetadata> projects)
  {
    projects.ForEach(project =>
    {
      MSBuild(s => s
          .SetProperty(MSBuildProperties.RestorePackagesConfig, true)
          .SetProperty(MSBuildProperties.SolutionDir, Directories.Solution)
          .SetTargetPath(project.ProjectPath)
          .SetTargets(MSBuildTargets.Restore)
      );
    });
  }
}