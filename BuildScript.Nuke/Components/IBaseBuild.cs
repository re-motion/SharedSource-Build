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
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Remotion.BuildScript.Components;

public interface IBaseBuild : INukeBuild
{
  private static readonly string[] s_defaultBuildConfigurations = new[] { "Debug", "Release" };

  [Solution]
  public Solution Solution => TryGetValue(() => Solution) ?? throw new InvalidOperationException("The solution is currently unavailable.");

  [GitRepository]
  public GitRepository Repository => TryGetValue(() => Repository) ?? throw new InvalidOperationException("The git repository is currently unavailable.");

  public BuildType BuildType { get; }

  [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
  public string[] Configurations => TryGetValue(() => Configurations) ?? s_defaultBuildConfigurations;

  [Parameter("Path to the output folder where build artifacts are put.")]
  public AbsolutePath OutputFolder => TryGetValue(() => OutputFolder) ?? RootDirectory / "BuildOutput";

  [Parameter("Path to the temp folder where temporary build files are put.")]
  public AbsolutePath TempFolder => TryGetValue(() => TempFolder) ?? OutputFolder / "Temp";

  [Parameter("Path to the log folder where log files are put.")]
  public AbsolutePath LogFolder => TryGetValue(() => LogFolder) ?? OutputFolder / "Log";

  public AbsolutePath CustomizationsFolder => BuildProjectDirectory / "Customizations";
}