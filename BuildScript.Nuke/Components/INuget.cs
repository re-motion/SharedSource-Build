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
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Utilities.Collections;
using Remotion.BuildScript.Components.Tasks;

namespace Remotion.BuildScript.Components;

public interface INuget : IBaseBuild
{
  [Parameter("Skip generation of nuget package with debug symbols - true / false")]
  protected bool SkipNuGet => TryGetValue<bool?>(() => SkipNuGet) ?? false;

  [Parameter("Skip generation of nuget package with symbol server support - true / false")]
  protected bool SkipNuGetOrg => TryGetValue<bool?>(() => SkipNuGetOrg) ?? false;

  [PublicAPI]
  public Target GenerateNuGetPackagesWithDebugSymbols => _ => _
      .DependsOn<IReadConfiguration>(x => x.ReadConfiguration)
      .DependsOn<ICompile>(x => x.CompileReleaseBuild, x => x.CompileTestBuild)
      .Description("Generate nuget packages with debug symbols")
      .OnlyWhenStatic(() => !SkipNuGet)
      .Executes(() =>
      {
        ConfigurationData.ReleaseProjectFiles.ForEach(projectFile =>
        {
          NugetTask.GenerateSinglePackageWithDebugSymbols(
              projectFile,
              ConfigurationData.SemanticVersion,
              ConfigurationData.AssemblyMetadata,
              Directories);
        });
      });

  [PublicAPI]
  public Target GenerateNuGetPackagesWithSymbolServerSupport => _ => _
      .DependsOn<IReadConfiguration>(x => x.ReadConfiguration)
      .DependsOn<ICompile>(x => x.CompileReleaseBuild, x => x.CompileTestBuild)
      .Description("Generate nuget packages with symbol server support")
      .OnlyWhenStatic(() => !SkipNuGetOrg)
      .Executes(() =>
      {
        ConfigurationData.ReleaseProjectFiles.ForEach(projectFile =>
        {
          NugetTask.GenerateSinglePackageWithSymbolServerSupport(
              projectFile,
              ConfigurationData.SemanticVersion,
              ConfigurationData.AssemblyMetadata,
              Directories);
        });
      });
}
