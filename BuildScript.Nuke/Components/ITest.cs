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

using System.IO;
using System.Net.NetworkInformation;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Remotion.BuildScript.TestMatrix;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface ITest : IBuild, IProjectMetadata, ITestMatrix
{
  [PublicAPI]
  public Target Test => _ => _
      .DependsOn<IProjectMetadata>()
      .DependsOn<ITestMatrix>()
      .DependsOn<IBuild>()
      .Description("Runs all tests")
      .Executes(() =>
      {
        foreach (var projectMetadata in ProjectMetadata)
        {
          var testMatrix = projectMetadata.GetTestMatrixOrDefault();
          if (testMatrix == null)
            continue;

          Log.Information($"Testing project '{projectMetadata.Name}' in {testMatrix.TestConfigurations.Length} test configurations.");
          foreach (var testConfiguration in testMatrix.TestConfigurations)
          {
            Log.Information($"Run test configuration '{testConfiguration}':");

            var assemblyFileName = Path.GetFileName(projectMetadata.Path);
            var logFileName = $"{assemblyFileName}.{string.Join(".", testConfiguration.Elements)}";

            // todo included/excluded categories -> test filter
            var testSettings = new DotNetTestSettings()
                .SetProjectFile(projectMetadata.Path)
                .When(TeamCity.Instance != null, settings => settings.AddTeamCityLogger())
                .AddLoggers($"trx;LogFileName={logFileName}")
                .EnableNoRestore()
                .EnableNoBuild();

            foreach (var configure in testConfiguration.Elements.OfType<IConfigureTestSettings>())
              testSettings = configure.ConfigureTestSettings(testSettings);

            // todo add docker support
            var process = ProcessTasks.StartProcess(testSettings);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
              Log.Error($"Test execution failed with exit code '{process.ExitCode}'");
            }
            else
            {
              Log.Information("Test execution finished.");
            }
          }
        }
      });
}