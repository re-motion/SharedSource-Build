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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Remotion.BuildScript.Test;
using Serilog;

namespace Remotion.BuildScript;

public class ProjectsBuilder
{
  private readonly Solution _solution;

  private readonly Dictionary<string, ProjectBuilder> _projects = new();

  public ProjectsBuilder (Solution solution)
  {
    ArgumentNullException.ThrowIfNull(solution);

    _solution = solution;
  }

  public ProjectBuilder AddProject (string name)
  {
    ArgumentNullException.ThrowIfNull(name);

    var project = _solution.AllProjects.SingleOrDefault(e => e.Name == name);
    if (project == null)
      throw new InvalidOperationException($"The project '{name}' could not be found in the solution.");

    var msBuildProject = ProjectModelTasks.ParseProject(project.Path);

    var projectBuilder = new ProjectBuilder(project.Name, project.Path, msBuildProject);
    _projects.Add(name, projectBuilder);

    var targetFrameworks = msBuildProject.GetProperty("TargetFrameworks")?.EvaluatedValue
                           ?? msBuildProject.GetProperty("TargetFramework")?.EvaluatedValue;
    targetFrameworks.NotNull($"Could not determine target framework for project '{project.Name}'");
    projectBuilder.SetMetadata(RemotionBuildMetadataProperties.TargetFrameworks, TargetFrameworkSet.Parse(targetFrameworks!));

    Log.Information($"Determined project metadata for project '{project.Name}'.");

    return projectBuilder;
  }

  public ProjectBuilder AddReleaseProject (string name)
  {
    ArgumentNullException.ThrowIfNull(name);

    return AddProject(name)
        .SetMetadata(RemotionBuildMetadataProperties.CreateNugetPackage, true);
  }

  public ProjectBuilder AddUnitTestProject (string name, TestConfiguration testConfiguration)
  {
    ArgumentNullException.ThrowIfNull(name);

    return AddProject(name)
        .SetMetadata(RemotionBuildMetadataProperties.TestConfiguration, testConfiguration);
  }

  public ImmutableArray<ProjectMetadata> Build ()
  {
    return _projects.Values.Select(e => e.Build()).ToImmutableArray();
  }
}