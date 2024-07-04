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
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;

namespace Remotion.BuildScript.Components;

public interface IBuild : IBaseBuild, IBuildMetadata, IRestore
{
  [Parameter("Path to the key file containing the signing key.")]
  public AbsolutePath AssemblySigningKeyFile => TryGetValue(() => AssemblySigningKeyFile) ?? (RootDirectory / "remotion.snk");

  [PublicAPI]
  public Target Build => _ => _
      .DependsOn<IBuildMetadata>()
      .DependsOn<IRestore>()
      .After<IClean>()
      .Description("Builds all projects")
      .Executes(() =>
      {
        Assert.FileExists(AssemblySigningKeyFile);

        Configurations.ForEach(configuration =>
        {
          var buildMetadata = GetBuildMetadata(configuration);
          DotNetTasks.DotNetBuild(s => s
              .SetProjectFile(Solution)
              .SetConfiguration(configuration)
              .EnableNoRestore()
              .SetAssemblyVersion(buildMetadata.AssemblyVersion)
              .SetFileVersion(buildMetadata.AssemblyFileVersion)
              .SetInformationalVersion(buildMetadata.AssemblyInformationalVersion)
              .SetProperty("PackageVersion", buildMetadata.AssemblyNuGetVersion)
              .SetProperty("AssemblyOriginatorKeyFile", AssemblySigningKeyFile)
          );
        });
      });
}