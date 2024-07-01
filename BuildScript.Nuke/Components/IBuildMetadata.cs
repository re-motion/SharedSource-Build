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
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities.Collections;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface IBuildMetadata : IBaseBuild
{
  public bool UseReleaseVersioning { get; set; }

  public ImmutableDictionary<string, BuildMetadata> BuildMetadataPerConfiguration { get; set; }

  [Parameter("Additional build metadata that is attached to the assembly informational version.")]
  public string AdditionalBuildMetadata => TryGetValue(() => AdditionalBuildMetadata) ?? $"Commit-{DetermineCommitHash()}";

  [PublicAPI]
  public Target DetermineBuildMetadata => _ => _
      .Description("Determines the build metadata, for example, build versions.")
      .Unlisted()
      .Executes(() =>
      {
        var project = ProjectModelTasks.ParseProject(CustomizationsFolder / "Version.props");
        var versionString = project.Properties.Single(e => e.Name == "Version").EvaluatedValue;

        var buildNumber = TeamCity.Instance?.BuildNumber ?? "0";
        var remotionBuildVersion = RemotionBuildVersionFormatter.Instance.FormatRemotionBuildVersion(
            versionString,
            IsServerBuild,
            UseReleaseVersioning,
            buildNumber);
        TeamCity.Instance?.SetBuildNumber(remotionBuildVersion.Version);

        var buildMetadataPerConfigurationBuilder = ImmutableDictionary.CreateBuilder<string, BuildMetadata>();
        Configurations.ForEach(configuration =>
        {
          var buildMetadata = CreateBuildMetadataForConfiguration(configuration, remotionBuildVersion);
          buildMetadataPerConfigurationBuilder[configuration] = buildMetadata;
        });
        BuildMetadataPerConfiguration = buildMetadataPerConfigurationBuilder.ToImmutable();

        Log.Information($"Determined build metadata ({nameof(UseReleaseVersioning)} = {UseReleaseVersioning}).");
        Configurations.ForEach(configuration =>
        {
          var buildMetadata = GetBuildMetadata(configuration);
          Log.Information($"Build metadata for configuration '{configuration}':");
          Log.Information($"  {nameof(BuildMetadata.Version)}: {buildMetadata.Version}");
          Log.Information($"  {nameof(BuildMetadata.AssemblyVersion)}: {buildMetadata.AssemblyVersion}");
          Log.Information($"  {nameof(BuildMetadata.AssemblyFileVersion)}: {buildMetadata.AssemblyFileVersion}");
          Log.Information($"  {nameof(BuildMetadata.AssemblyNuGetVersion)}: {buildMetadata.AssemblyNuGetVersion}");
          Log.Information($"  {nameof(BuildMetadata.AssemblyInformationalVersion)}: {buildMetadata.AssemblyInformationalVersion}");
          Log.Information($"  {nameof(BuildMetadata.AdditionalBuildMetadata)}: {buildMetadata.AdditionalBuildMetadata}");
        });
      });

  public BuildMetadata CreateBuildMetadataForConfiguration (string configuration, RemotionBuildVersion remotionBuildVersion)
  {
    var additionalBuildMetadata = AdditionalBuildMetadata;
    var assemblyInformationalVersion = RemotionBuildVersionFormatter.Instance.FormatAssemblyInformationalVersion(
        remotionBuildVersion.Version,
        configuration,
        additionalBuildMetadata);

    return new BuildMetadata(
        remotionBuildVersion.Version,
        remotionBuildVersion.AssemblyVersion,
        remotionBuildVersion.AssemblyFileVersion,
        remotionBuildVersion.AssemblyNugetVersion,
        assemblyInformationalVersion,
        additionalBuildMetadata);
  }

  public BuildMetadata GetBuildMetadata (string configuration) => BuildMetadataPerConfiguration[configuration];

  private string DetermineCommitHash ()
  {
    return TeamCity.Instance != null
        ? TeamCity.Instance.BuildVcsNumber
        : Repository?.Commit ?? "";
  }
}