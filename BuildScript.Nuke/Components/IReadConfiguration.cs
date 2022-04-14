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
using System.Linq;
using Nuke.Common;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface IReadConfiguration : IBaseBuild
{
  public Target ReadConfiguration => _ => _
      .Unlisted()
      .Executes(() =>
      {
        var projectProps = ProjectPropsReader.Read(Solution, Configuration, RootDirectory, Directories.CustomizationPath);
        var buildConfigurationProps = BuildConfigurationPropsReader.Read(RootDirectory, Directories.CustomizationPath);
        ConfigurationData = new ConfigurationData
                            {
                                ReleaseProjectFiles = projectProps.ReleaseProjectMetadata,
                                UnitTestProjectFiles = projectProps.UnitTestProjectMetadata,
                                IntegrationTestProjectFiles = projectProps.IntegrationTestProjectMetadata,
                                NormalTestConfiguration = projectProps.NormalTestConfiguration,
                                TestProjectFiles = projectProps.UnitTestProjectMetadata.Concat(projectProps.IntegrationTestProjectMetadata)
                                    .ToList(),

                                SupportedTargetRuntimes = buildConfigurationProps.SupportedTargetRuntimes,
                                SupportedBrowsers = buildConfigurationProps.SupportedBrowsers,
                                SupportedPlatforms = buildConfigurationProps.SupportedPlatforms,
                                SupportedDatabaseSystems = buildConfigurationProps.SupportedDatabaseSystems,
                                SupportedExecutionRuntimes = buildConfigurationProps.SupportedExecutionRuntimes,

                                SemanticVersion = VersionPropsReader.Read(RootDirectory, Directories.CustomizationPath, IsLocalBuild),
                                AssemblyMetadata = PropertiesPropsReader.Read(RootDirectory, Directories.CustomizationPath)
                            };

        Log.Information("Read configuration data successfully");
      });
}
