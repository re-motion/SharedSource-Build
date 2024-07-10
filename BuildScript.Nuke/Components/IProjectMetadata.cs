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

using System.Collections.Immutable;
using JetBrains.Annotations;
using Nuke.Common;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface IProjectMetadata : IBaseBuild
{
  public ImmutableArray<ProjectMetadata> ProjectMetadata { get; set; }

  [PublicAPI]
  public Target DetermineProjectMetadata => _ => _
      .TryDependsOn<ITestMatrix>()
      .Description("Determines metadata for projects in the solution")
      .Executes(() =>
      {
        var projects = new ProjectsBuilder(Solution);
        ConfigureProjects(projects);

        var projectMetadata = projects.Build();

        ProjectMetadata = projectMetadata;
        Log.Information($"Determined project metadata for {projectMetadata.Length} projects.");
      });

  void ConfigureProjects (ProjectsBuilder projects);
}