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
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities.Collections;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface IBuildVersion : IBaseBuild
{
  private static VersionDetails? s_versionDetails;

  public static void SetVersionDetails (VersionDetails versionDetails) => s_versionDetails = versionDetails;

  public string Version => s_versionDetails?.Version ?? throw new InvalidOperationException("Build versions were not set yet.");

  public string AssemblyVersion => s_versionDetails?.AssemblyVersion ?? throw new InvalidOperationException("Build versions were not set yet.");

  public string AssemblyFileVersion => s_versionDetails?.AssemblyFileVersion ?? throw new InvalidOperationException("Build versions were not set yet.");

  public string AssemblyNuGetVersion => s_versionDetails?.AssemblyNugetVersion ?? throw new InvalidOperationException("Build versions were not set yet.");

  public string AdditionalBuildMetadata => $"Commit-{(TeamCity.Instance?.BuildVcsNumber ?? Repository.Head).NotNull()}";

  public string GetAssemblyInformationalVersion (string configuration, string additionalBuildMetadata)
  {
    return string.IsNullOrEmpty(additionalBuildMetadata)
        ? $"{Version}+{configuration}"
        : $"{Version}+{configuration}.{additionalBuildMetadata}";
  }

  [PublicAPI]
  public Target DetermineBuildVersion => _ => _
      .Description("Reads Version.props and calculates different versions.")
      .Unlisted()
      .Executes(() =>
      {
        var project = ProjectModelTasks.ParseProject(CustomizationsFolder / "Version.props");
        var versionString = project.Properties.Single(e => e.Name == "Version").EvaluatedValue;

        var buildNumber = TeamCity.Instance?.BuildNumber ?? "0";
        var versionDetails = RemotionBuildVersionFormatter.FormatVersions(versionString, BuildType, buildNumber);
        SetVersionDetails(versionDetails);

        TeamCity.Instance?.SetBuildNumber(versionDetails.Version);

        Log.Information("Determined build version:");
        Log.Information($"{nameof(Version)}: {versionDetails.Version}");
        Log.Information($"{nameof(AssemblyVersion)}: {versionDetails.AssemblyVersion}");
        Log.Information($"{nameof(AssemblyFileVersion)}: {versionDetails.AssemblyFileVersion}");
        Log.Information($"{nameof(AssemblyNuGetVersion)}: {versionDetails.AssemblyNugetVersion}");
        Configurations.ForEach(configuration =>
        {
          Log.Information($"AssemblyInformationalVersion ({configuration}): {GetAssemblyInformationalVersion(configuration, AdditionalBuildMetadata)}");
        });
      });
}