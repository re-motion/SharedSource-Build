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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Remotion.BuildScript.GenerateSbom;
using SbomCleaner.Library;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface IGenerateSbom: IBuild, IProjectMetadata
{
  [NuGetPackage(
      packageId: "CycloneDX",
      packageExecutable: "CycloneDX.dll")]
  Tool? CycloneDX => TryGetValue(() => CycloneDX);

  ISbomGeneratorBuilder ConfigureSbomGenerationInfoBuilder(Solution solution);

  [PublicAPI]
  public Target GenerateSbom => _ => _
      .DependsOn<IBuild>()
      .DependsOn<IProjectMetadata>()
      .Executes(() =>
      {
        var projects =
            ProjectMetadata
                .Select(p =>
                {
                  var outputPath = Path.Combine(
                    p.FolderPath,
                    p.GetMetadata(RemotionBuildMetadataProperties.OutputPath),
                    p.GetMetadata(RemotionBuildMetadataProperties.TargetFrameworks).First());

                  var assemblyName = p.GetMetadata(RemotionBuildMetadataProperties.AssemblyName);
                  return new ProjectInfo(assemblyName, outputPath);
                })
                .ToList();

        var sbomGenerationInfo = ConfigureSbomGenerationInfoBuilder(Solution).Build();
        if (sbomGenerationInfo.SbomGeneration is SbomGeneration.None)
        {
          Log.Information("Sbom generation has not been configured and will therefore not run.");
          return;
        }

        if (CycloneDX == null)
        {
          Log.Warning("Could not create sbom as the cyclone DX tool is missing.");
          return;
        }

        sbomGenerationInfo.Generate(CycloneDX, projects);
      });
}