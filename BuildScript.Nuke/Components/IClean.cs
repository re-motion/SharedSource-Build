﻿// Copyright (c) rubicon IT GmbH, www.rubicon.eu
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
using Nuke.Common.IO;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface IClean : IBaseBuild
{
  [PublicAPI]
  public Target Clean => _ => _
      .Description("Remove build output, log and temp folders")
      .Executes(() =>
      {
        Log.Information($"Cleaning output folder '{OutputFolder}'.");
        OutputFolder.CreateOrCleanDirectory();
        Log.Information($"Cleaning log folder '{LogFolder}'.");
        LogFolder.CreateOrCleanDirectory();
        Log.Information($"Cleaning temp folder '{TempFolder}'.");
        TempFolder.CreateOrCleanDirectory();

        var allProjectExceptBuildProject = Solution.AllProjects
            .Where(e => e.Path != BuildProjectFile);
        foreach (var project in allProjectExceptBuildProject)
        {
          (project.Directory / "bin").DeleteDirectory();
          (project.Directory / "obj").DeleteDirectory();
        }
      });
}