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
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Remotion.BuildScript.Components.Tasks;
using Remotion.BuildScript.GenerateTestMatrix;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface IRunTests : IBaseBuild
{
  [Parameter("Browser available for the build to use for test running")]
  protected string[] Browsers => TryGetValue(() => Browsers) ?? new[] { "NoBrowser" };

  [Parameter("Target runtimes available for the build to use for test running")]
  protected string[] TargetRuntimes => TryGetValue(() => TargetRuntimes) ?? new[] { "NET48", "NET45", "NET50", "NET461" };

  [Parameter("Execution runtimes available for the build to use for test running")]
  protected string[] ExecutionRuntimes => TryGetValue(() => ExecutionRuntimes) ?? new[] { "LocalMachine" };

  [Parameter("Database runtimes available for the build to use for test running")]
  protected string[] DatabaseSystems => TryGetValue(() => DatabaseSystems) ?? new[] { "NoDB" };

  [Parameter("Platforms available for the build to use for test running")]
  protected string[] Platforms => TryGetValue(() => Platforms) ?? new[] { "x86", "x64" };

  [Parameter("Path to MSBuildExtensionPack for running the TestSetupBuildFile")]
  protected string MsBuildExtensionPackPath => TryGetValue(() => MsBuildExtensionPackPath) ?? "";

  [PublicAPI]
  public Target RunTests => _ => _
      .DependsOn<ICompile>(x => x.CompileTestBuild)
      .DependsOn<ICleanup>(x => x.CleanFolders)
      .Description("Run all tests")
      .OnlyWhenStatic(() => !SkipTests)
      .Executes(() =>
          {
            var createTestConfigurations = new CreateTestConfigurations(
                supportedBrowsers: ConfigurationData.SupportedBrowsers,
                supportedPlatforms: ConfigurationData.SupportedPlatforms,
                supportedDatabaseSystems: ConfigurationData.SupportedDatabaseSystems,
                supportedExecutionRuntimes: ConfigurationData.SupportedExecutionRuntimes,
                supportedTargetRuntimes: ConfigurationData.SupportedTargetRuntimes,
                enabledBrowsers: Browsers,
                enabledDatabaseSystems: DatabaseSystems,
                enabledTargetRuntimes: TargetRuntimes,
                enabledExecutionRuntimes: ExecutionRuntimes,
                enabledPlatforms: Platforms,
                enabledConfigurationIDs: Configuration,
                testOutputFiles: ConfigurationData.TestProjectFiles
            );
            var output = createTestConfigurations.CreateTestMatrix();
            if (output.Length == 0)
            {
              Log.Warning("The created test matrix from the test configuration is empty!");
              return;
            }

            RunTestsTask.LogTestMatrix(output);

            var dockerTestConfigs = output.Where(config => config.ExecutionRuntime.UseDocker).ToList();
            RunTestsTask.PrepareDockerImages(dockerTestConfigs);

            FileSystemTasks.EnsureExistingDirectory(Directories.Log);
            Log.Information("Start running tests");
            var beforeTestTime = DateTime.Now;
            var outputResult = output.Select(testConfig => RunTestsTask.RunTest(
                testConfig,
                Directories,
                MsBuildExtensionPackPath)).ToList();
            if (outputResult.Any(testConfig => testConfig == -1))
              Assert.Fail($"{outputResult.Sum(x => x == -1 ? 1 : 0)} Test(s) had errors!");
            if (outputResult.Any(testConfig => testConfig != 0))
              Assert.Fail($"{outputResult.Sum(x => x > 0 ? x : 0)} Test(s) failed!");
            var runTestTime = DateTime.Now - beforeTestTime;
            Log.Information("Test run time: {RunTestTime}", runTestTime);
          }
      );
}
