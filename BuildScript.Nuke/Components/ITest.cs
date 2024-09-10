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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

public interface ITest : IBuild, IProjectMetadata, ITestMatrix, ITestParameters
{
  [Parameter("Executes only tests that match the specified test filter.")]
  public string TestFilter => TryGetValue(() => TestFilter) ?? "";

  [PublicAPI]
  public Target Test => _ => _
      .DependsOn<IProjectMetadata>()
      .DependsOn<ITestMatrix>()
      .DependsOn<ITestParameters>()
      .DependsOn<IBuild>()
      .Description("Runs all tests")
      .Executes(() =>
      {
        var testGroups = CreateTestGroups();
        // todo print test grouping

        var testContext = CreateTestContext();
        foreach (var testGroup in testGroups)
          testGroup.Execute(testContext);

        var finalMessage = $"Test execution finished. Failed: {testContext.FailedTestCount},"
                           + $" Passed: {testContext.PassedTestCount}, Total: {testContext.TotalTestCount}";
        if (testContext.FailedTestCount > 0)
        {
          Log.Error(finalMessage);
        }
        else
        {
          Log.Information(finalMessage);
        }

        Assert.False(testContext.FatalFailure, "One or more test projects failed fatally.");
      });

  ITestContext CreateTestContext () => new TestContext(this);

  ImmutableArray<TestGroup> CreateTestGroups ()
  {
    var testGroups = ImmutableArray.CreateBuilder<TestGroup>();
    var testCases = ImmutableArray.CreateBuilder<ITestItem>();

    foreach (var projectMetadata in ProjectMetadata)
    {
      var testConfiguration = projectMetadata.GetMetadataOrDefault(RemotionBuildMetadataProperties.TestConfiguration);
      if (testConfiguration == null)
        continue;

      var testMatrix = testConfiguration.TestMatrix;
      if (testMatrix.IsEmpty)
      {
        Log.Information($"Skipped test project '{projectMetadata.Name}' as there are no test configurations.");
        continue;
      }

      var supportedTargetFrameworks = projectMetadata.GetMetadata(RemotionBuildMetadataProperties.TargetFrameworks);
      foreach (var row in testMatrix.Rows)
      {
        var targetFramework = row.GetDimensionOrDefault<TargetFrameworks>();
        if (targetFramework != null && !supportedTargetFrameworks.Contains(targetFramework.Identifier))
        {
          Log.Warning($"Skipped test configuration '{row}' as the target framework '{targetFramework.Identifier}' is not supported. "
                      + $"Supported are: '{supportedTargetFrameworks}'");
          continue;
        }

        var testCase = new TestCase(
            row.ToString(),
            projectMetadata,
            row,
            testConfiguration.TestExecutionRuntimeFactory,
            testConfiguration.TestExecutionWrappers);
        testCases.Add(testCase);
      }

      if (testCases.Count > 0)
      {
        testGroups.Add(new SerialTestGroup(projectMetadata.Name, testCases.ToImmutable()));
        testCases.Clear();
      }
    }

    return testGroups.ToImmutable();
  }
}