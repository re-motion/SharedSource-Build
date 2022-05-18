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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;

namespace Remotion.BuildScript.Components.Tasks;

internal static class CompileTask
{
  internal static void CompileProjects (
      IReadOnlyCollection<ProjectMetadata> projectFiles,
      Directories directories,
      string additionalBuildMetadata,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      string msBuildNukePath,
      string msBuildPath,
      VisualStudioVersion? visualStudioVersion,
      string solutionPath
  )
  {
    var toolPath = _toolPath ??= BaseTask.GetMsBuildToolPath(msBuildNukePath, msBuildPath, visualStudioVersion);
    var sortedProjects = _sortedProjects ??= GetSortedProjects(solutionPath).GetAwaiter().GetResult();

    var projectFilesDebugTargets = projectFiles
        .Where(file => file.Configuration == "Debug").ToList();
    var projectFilesReleaseTargets = projectFiles
        .Where(file => file.Configuration == "Release").ToList();
    if (projectFilesDebugTargets.Count > 0)
    {
      CompileProject(
          projectFilesDebugTargets,
          "Debug",
          directories,
          additionalBuildMetadata,
          semanticVersion,
          assemblyMetadata,
          toolPath,
          sortedProjects);
    }

    if (projectFilesReleaseTargets.Count > 0)
    {
      CompileProject(
          projectFilesReleaseTargets,
          "Release",
          directories,
          additionalBuildMetadata,
          semanticVersion,
          assemblyMetadata,
          toolPath,
          sortedProjects);
    }
  }

  internal static async Task<IReadOnlyCollection<Project>> GetSortedProjects (string solutionPath)
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

  private static string? _toolPath;
  private static IReadOnlyCollection<Project>? _sortedProjects;

  private static void CompileProject (
      IReadOnlyCollection<ProjectMetadata> projects,
      string configuration,
      Directories directories,
      string additionalBuildMetadata,
      SemanticVersion semanticVersion,
      AssemblyMetadata assemblyMetadata,
      string toolPath,
      IReadOnlyCollection<Project> sortedProjectList)
  {
    var filteredProjects = sortedProjectList
        .Select(projectSort => projects
            .SingleOrDefault(project => projectSort.FilePath == project.ProjectPath))
        .WhereNotNull()
        .Distinct();

    filteredProjects.ForEach(project =>
    {
      MSBuildTasks.MSBuild(s => s
          .SetProcessToolPath(toolPath)
          .SetAssemblyVersion(semanticVersion.AssemblyVersion)
          .SetFileVersion(semanticVersion.AssemblyFileVersion)
          .SetCopyright(assemblyMetadata.Copyright)
          .SetProperty(MSBuildProperties.PackageVersion, semanticVersion.AssemblyNuGetVersion)
          .SetProperty(MSBuildProperties.CompanyName, assemblyMetadata.CompanyName)
          .SetProperty(MSBuildProperties.CompanyUrl, assemblyMetadata.CompanyUrl)
          .SetProperty(MSBuildProperties.ProductName, assemblyMetadata.ProductName)
          .SetProperty(MSBuildProperties.AssemblyOriginatorKeyFile, directories.SolutionKeyFile)
          .SetToolsVersion(project!.ToolsVersion)
          .SetConfiguration(configuration)
          .SetInformationalVersion(
              semanticVersion.GetAssemblyInformationalVersion(configuration, additionalBuildMetadata))
          .SetTargetPath(project.ProjectPath)
          .SetTargets(project.IsMultiTargetFramework ? MSBuildTargets.DispatchToInnerBuilds : MSBuildTargets.Build)
      );
    });
  }
}
