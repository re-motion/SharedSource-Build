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
using System.Collections.Immutable;
using System.Reflection;
using Nuke.Common;
using Remotion.BuildScript.Components;
using Serilog;

namespace Remotion.BuildScript;

public abstract partial class RemotionBuild
    : NukeBuild,
        IClean,
        IRestore,
        IBuild,
        IPack,
        ITest
{
  ImmutableDictionary<string, BuildMetadata> IBuildMetadata.BuildMetadataPerConfiguration { get; set; } = ImmutableDictionary<string, BuildMetadata>.Empty;

  [Parameter("Uses release instead of debug versioning when determining the build versions.")]
  public bool UseReleaseVersioning { get; set; }

  public ImmutableArray<ProjectMetadata> ProjectMetadata { get; set; }

  protected override void OnBuildInitialized ()
  {
    var buildScriptVersion = typeof(RemotionBuild).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
    Log.Information($"re-motion NUKE Build Script version {buildScriptVersion}");
    Log.Information($"IsServerBuild: {(IsServerBuild ? "True" : "False")}");
    Log.Information($"Host: {Host?.GetType().Name ?? "n/a"}");
  }
}