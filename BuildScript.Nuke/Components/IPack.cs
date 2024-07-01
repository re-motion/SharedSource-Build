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

using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;

namespace Remotion.BuildScript.Components;

public interface IPack : IBuild, IBuildMetadata, IProjectMetadata
{
  [PublicAPI]
  public Target Pack => _ => _
      .DependsOn<IBuildMetadata>()
      .DependsOn<IBuild>()
      .DependsOn<IProjectMetadata>()
      .Description("Packages the projects")
      .Executes(() =>
      {
        Configurations.ForEach(configuration =>
        {
          ProjectMetadata.Where(e => e.GetMetadata(RemotionBuildMetadataProperties.CreateNugetPackage)).ForEach(project =>
          {
            var buildMetadata = GetBuildMetadata(configuration);
            DotNetTasks.DotNetPack(s => s
                .SetProject(project.FilePath)
                .SetConfiguration(configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .EnableContinuousIntegrationBuild()
                .SetVersion(buildMetadata.AssemblyNuGetVersion)
                .SetOutputDirectory(OutputFolder / "NuGetWithDebugSymbols" / configuration)
            );
          });
        });
      });
}