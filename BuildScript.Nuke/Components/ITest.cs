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
using Nuke.Common.Tools.DotNet;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Test.Dimensions;
using Remotion.BuildScript.Util;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface ITest : IBuild, IProjectMetadata, ITestMatrix, ITestSettings
{
  [PublicAPI]
  public Target Test => _ => _
      .DependsOn<IProjectMetadata>()
      .DependsOn<ITestMatrix>()
      .DependsOn<ITestSettings>()
      .DependsOn<IBuild>()
      .Description("Runs all tests")
      .Executes(() =>
      {
        var testsFailed = false;
        int passedTestCount = 0, failedTestCount = 0, totalTestCount = 0;
        foreach (var projectMetadata in ProjectMetadata)
        {
          var testMatrix = projectMetadata.GetMetadataOrDefault(RemotionBuildMetadataProperties.TestMatrix);
          if (testMatrix == null)
            continue;

          using var _ = GroupingBlock.Start($"Test project '{projectMetadata.Name}' with {testMatrix.TestConfigurations.Length} test configurations");
          Log.Information($"Testing project '{projectMetadata.Name}' in {testMatrix.TestConfigurations.Length} test configurations.");
          foreach (var testConfiguration in testMatrix.TestConfigurations)
          {
            using var __ = GroupingBlock.Start($"Test configuration '{testConfiguration}'");
            Log.Information($"Run test configuration '{testConfiguration}':");

            var resultFileName = $"{projectMetadata.Name}.{string.Join(".", testConfiguration.Elements)}.xml";
            var resultFilePath = LogFolder / resultFileName;

            // todo included/excluded categories -> test filter
            var dotNetTestSettings = new DotNetTestSettings()
                .SetProjectFile(projectMetadata.Path)
                .AddLoggers($"trx;LogFileName={resultFilePath}")
                .EnableNoRestore()
                .EnableNoBuild();

            foreach (var configure in testConfiguration.Elements.OfType<IConfigureTestSettings>())
              dotNetTestSettings = configure.ConfigureTestSettings(dotNetTestSettings);

            var executionRuntime = testConfiguration.GetDimensionOrDefault<ExecutionRuntimes>()
                ?? ExecutionRuntimes.LocalMachine;

            var testExecutionRuntime = TestSettings.ExecutionRuntimeFactory.CreateTestExecutionRuntime(executionRuntime);

            var testExecutionContext = new TestExecutionContext(this, projectMetadata, testConfiguration, TestSettings, dotNetTestSettings);
            var exitCode = testExecutionRuntime.ExecuteTests(testExecutionContext);

            if (exitCode != 0)
            {
              Log.Error($"Test execution failed with exit code '{exitCode}'");
            }
            else
            {
              Log.Information("Test execution finished.");
            }

            TeamCity.Instance?.ImportData(TeamCityImportType.mstest, resultFilePath, verbose: true, action: TeamCityNoDataPublishedAction.error);
            Assert.True(resultFilePath.FileExists());

            passedTestCount += int.Parse(XmlTasks.XmlPeekSingle(resultFilePath, "//@passed")!);
            failedTestCount += int.Parse(XmlTasks.XmlPeekSingle(resultFilePath, "//@failed")!);
            totalTestCount += int.Parse(XmlTasks.XmlPeekSingle(resultFilePath, "//@total")!);
          }
        }

        if (testsFailed)
        {
          Log.Error($"Tests failed. Failed: {failedTestCount}, Passed: {passedTestCount}, Total: {totalTestCount}");
        }
        else
        {
          Log.Information($"Tests succeeded. Failed: {failedTestCount}, Passed: {passedTestCount}, Total: {totalTestCount}");
        }

        if (testsFailed)
          throw new InvalidOperationException("Test execution failed.");
      });
}