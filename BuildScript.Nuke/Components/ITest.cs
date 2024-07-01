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
using Nuke.Common.CI.TeamCity;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Test.Dimensions;
using Remotion.BuildScript.Util;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface ITest : IBuild, IProjectMetadata, ITestMatrix, ITestSettings
{
  [Parameter("Executes only tests that match the specified test filter.")]
  public string TestFilter => TryGetValue(() => TestFilter) ?? "";

  [PublicAPI]
  public Target Test => _ => _
      .DependsOn<IProjectMetadata>()
      .DependsOn<ITestMatrix>()
      .DependsOn<ITestSettings>()
      .DependsOn<IBuild>()
      .Description("Runs all tests")
      .Executes(() =>
      {
        bool fatalFailure = false;
        int passedTestCount = 0, failedTestCount = 0, totalTestCount = 0;
        foreach (var projectMetadata in ProjectMetadata)
        {
          var testMatrix = projectMetadata.GetMetadataOrDefault(RemotionBuildMetadataProperties.TestMatrix);
          if (testMatrix == null)
            continue;

          if (testMatrix.IsEmpty)
          {
            Log.Information($"Skipped test project '{projectMetadata.Name}' as there are no test configurations");
            continue;
          }

          var supportedTargetFrameworks = projectMetadata.GetMetadata(RemotionBuildMetadataProperties.TargetFrameworks);

          using var _ = GroupingBlock.Start($"Test project '{projectMetadata.Name}' with {testMatrix.TestConfigurations.Length} test configurations");
          Log.Information($"Testing project '{projectMetadata.Name}' in {testMatrix.TestConfigurations.Length} test configurations.");
          foreach (var testConfiguration in testMatrix.TestConfigurations)
          {
            var targetFramework = testConfiguration.GetDimensionOrDefault<TargetFrameworks>();
            if (targetFramework != null && !supportedTargetFrameworks.Contains(targetFramework.Identifier))
            {
              Log.Warning($"Skipped test configuration '{testConfiguration}' as the target framework '{targetFramework.Identifier}' is not supported. "
                              + $"Supported are: '{supportedTargetFrameworks}'");
              continue;
            }

            using var __ = GroupingBlock.Start($"Test configuration '{testConfiguration}'");
            Log.Information($"Run test configuration '{testConfiguration}':");

            var resultFileName = $"{projectMetadata.Name}.{string.Join(".", testConfiguration.Elements)}.xml";
            var resultFilePath = LogFolder / resultFileName;

            var dotNetTestSettings = new DotNetTestSettings()
                .SetProjectFile(projectMetadata.FilePath)
                .AddLoggers($"trx;LogFileName={resultFilePath}")
                .EnableNoRestore()
                .EnableNoBuild()
                .When(!string.IsNullOrEmpty(TestFilter), s => s
                    .SetFilter(TestFilter)
                );

            foreach (var configure in testConfiguration.Elements.OfType<IConfigureTestSettings>())
              dotNetTestSettings = configure.ConfigureTestSettings(dotNetTestSettings);

            var executionRuntime = testConfiguration.GetDimensionOrDefault<ExecutionRuntimes>()
                ?? ExecutionRuntimes.LocalMachine;

            var testExecutionRuntime = TestSettings.ExecutionRuntimeFactory.CreateTestExecutionRuntime(executionRuntime);

            var testExecutionContext = new TestExecutionContext(this, projectMetadata, testConfiguration, TestSettings, dotNetTestSettings);
            var exitCode = testExecutionRuntime.ExecuteTests(testExecutionContext);

            // For unexpected exit code we want the build to fail after all tests are executed to ensure that the error is inspected
            if (exitCode != 0 && exitCode != 1)
            {
              Log.Fatal($"Test execution for '{projectMetadata.Name}' with '{testConfiguration}' failed with exit code {exitCode}.");
              fatalFailure = true;
              continue;
            }

            if (resultFilePath.FileExists())
            {
              var passedTests = int.Parse(XmlTasks.XmlPeekSingle(resultFilePath, "//@passed")!);
              var failedTests = int.Parse(XmlTasks.XmlPeekSingle(resultFilePath, "//@failed")!);
              var totalTests = int.Parse(XmlTasks.XmlPeekSingle(resultFilePath, "//@total")!);

              if (exitCode == 0)
              {
                Log.Information($"Test execution for '{projectMetadata.Name}' with '{testConfiguration}' succeeded. ({totalTests} tests)");
              }
              else
              {
                Log.Error($"Test execution for '{projectMetadata.Name}' with '{testConfiguration}' failed. ({failedTests}/{totalTests} failed tests)");
              }

              TeamCity.Instance?.ImportData(TeamCityImportType.mstest, resultFilePath, verbose: true, action: TeamCityNoDataPublishedAction.error);

              passedTestCount += passedTests;
              failedTestCount += failedTests;
              totalTestCount += totalTests;
            }
            else
            {
              if (exitCode == 0)
              {
                Log.Warning(
                    $"Test execution for '{projectMetadata.Name}' with '{testConfiguration}' did not produce an output file but reported exit code = 0. "
                    + "This can be correct if the target framework is not supported or if there are no tests to execute.");
              }
              else
              {
                Log.Error($"Test execution for '{projectMetadata.Name}' with '{testConfiguration}' did not produce any outputs.");
              }
            }
          }
        }

        var finalMessage = $"Test execution finished. Failed: {failedTestCount}, Passed: {passedTestCount}, Total: {totalTestCount}";
        if (failedTestCount > 0)
        {
          Log.Error(finalMessage);
        }
        else
        {
          Log.Information(finalMessage);
        }

        Assert.False(fatalFailure, "One or more test projects failed fatally.");
      });
}