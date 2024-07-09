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
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;

namespace Remotion.BuildScript.Components;

public interface IBaseBuild : INukeBuild
{
  public ConfigurationData ConfigurationData { get; set; }

  public Directories Directories => new(RootDirectory, BuildProjectDirectory);

  [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
  public string[] Configuration => TryGetValue(() => Configuration)
                                   ?? new[] { "Debug", "Release" };

  [Solution]
  public Solution Solution => TryGetValue(() => Solution);

  [Parameter("Added to the AssemblyInformationalVersion")]
  public string AdditionalBuildMetadata => TryGetValue(() => AdditionalBuildMetadata) ?? "";

  [Parameter("Skip compiling and running of tests - true / false")]
  public bool SkipTests => TryGetValue<bool?>(() => SkipTests) ?? false;
}
