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
using JetBrains.Annotations;
using Nuke.Common;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Util;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface ITest : ITestPlan, ITestParameters
{
  [Parameter("Executes only tests that match the specified test filter.")]
  public string TestFilter => TryGetValue(() => TestFilter) ?? "";

  [PublicAPI]
  public Target Test => _ => _
      .DependsOn<ITestPlan>()
      .DependsOn<ITestParameters>()
      .Description("Runs all tests")
      .Executes(() =>
      {
        var testParameters = TestParameters;
        List<ITestResource> testResources = [];
        try
        {
          foreach (var testResourceFactory in TestResourceFactories)
          {
            using var _ = GroupingBlock.Start($"Starting test resource '{testResourceFactory.Name}'.");
            testResources.Add(testResourceFactory.Start(this, testParameters));
          }

          var testContext = TestContextFactory.CreateTestContext(
              this,
              testParameters,
              [..testResources]);

          PreTest(testContext);
          foreach (var testItem in TestItems)
            testContext.ExecuteTestItem(testItem);
          PostTest(testContext);

          var finalMessage = $"Test execution finished. Failed: {testContext.FailedTestCount}, Passed: {testContext.PassedTestCount}, Total: {testContext.TotalTestCount}";
          if (testContext.FailedTestCount > 0)
          {
            Log.Error(finalMessage);
          }
          else
          {
            Log.Information(finalMessage);
          }

          Assert.False(testContext.FatalFailure, "One or more test projects failed fatally.");
        }
        finally
        {
          foreach (var testResource in testResources)
          {
            using var _ = GroupingBlock.Start($"Stopping test resource '{testResource.Name}'.");
            testResource.Dispose();
          }
        }
      });

  void PreTest (ITestContext testContext)
  {
  }

  void PostTest (ITestContext testContext)
  {
  }
}