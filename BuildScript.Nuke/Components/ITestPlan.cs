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
using JetBrains.Annotations;
using Nuke.Common;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Test.Dimensions;
using Remotion.BuildScript.TestPlan;
using Remotion.BuildScript.Util;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface ITestPlan : IBuild, ITestMatrix, IProjectMetadata
{
  public ImmutableArray<ITestItem> TestItems { get; set; }

  public ITestContextFactory TestContextFactory { get; set; }

  [PublicAPI]
  public Target CreateTestPlan => _ => _
      .DependsOn<IProjectMetadata>()
      .DependsOn<ITestMatrix>()
      .Executes(() =>
      {
        var testItemsBuilder = ImmutableArray.CreateBuilder<ITestItem>();
        var projectItemsBuilder = ImmutableArray.CreateBuilder<ITestItem>();

        Log.Information("Creating test plan...");
        foreach (var project in ProjectMetadata)
        {
          var testConfiguration = project.GetMetadataOrDefault(RemotionBuildMetadataProperties.TestConfiguration);
          if (testConfiguration == null)
            continue;

          var testMatrix = testConfiguration.TestMatrix;
          if (testMatrix.IsEmpty)
          {
            Log.Information($"Skipped test project '{project.Name}' as there are no test configurations");
            continue;
          }

          var supportedTargetFrameworks = project.GetMetadata(RemotionBuildMetadataProperties.TargetFrameworks);

          projectItemsBuilder.Clear();

          using var _ = GroupingBlock.Start($"Test project '{project.Name}' with {testMatrix.Rows.Length} test configurations");
          foreach (var testMatrixRow in testMatrix.Rows)
          {
            var targetFramework = testMatrixRow.GetDimensionOrDefault<TargetFrameworks>();
            if (targetFramework != null && !supportedTargetFrameworks.Contains(targetFramework.Identifier))
            {
              Log.Information($"Skipped test configuration '{testMatrixRow}' as the target framework '{targetFramework.Identifier}' is not supported. "
                          + $"Supported are: '{supportedTargetFrameworks}'");
              continue;
            }
            Log.Information($"Test configuration '{testMatrixRow}'");

            projectItemsBuilder.Add(CreateTestItem(project, testMatrixRow, testConfiguration));
          }

          if (projectItemsBuilder.Count > 0)
          {
            var projectTestGroup = new SerialTestGroup(project.Name, projectItemsBuilder.ToImmutable());
            testItemsBuilder.Add(projectTestGroup);
          }
        }

        TestItems = testItemsBuilder.ToImmutable();
        TestContextFactory = new DefaultTestContextFactory();
      });

  ITestItem CreateTestItem (ProjectMetadata project, TestMatrixRow testMatrixRow, TestConfiguration testConfiguration)
  {
    return new TestCase(project, testMatrixRow, testConfiguration);
  }
}