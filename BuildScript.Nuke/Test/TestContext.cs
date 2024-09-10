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
using Nuke.Common.CI.TeamCity;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Remotion.BuildScript.Components;
using Serilog;

namespace Remotion.BuildScript.Test;

public class TestContext : ITestContext
{
  private readonly ITest _iTest;

  public bool FatalFailure { get; private set; } = false;

  public int PassedTestCount { get; private set; }

  public int FailedTestCount { get; private set; }

  public int TotalTestCount { get; private set; }

  public TestContext (ITest iTest)
  {
    _iTest = iTest;
  }

  public void ExecuteTestCase (TestCase testCase)
  {
    Log.Information($"Run test configuration '{testCase.Row}':");

    var assemblyName = testCase.Project.GetMetadata(RemotionBuildMetadataProperties.AssemblyName);
    var resultFileName = $"{assemblyName}.{string.Join(".", testCase.Row.Elements)}.xml";
    var resultFilePath = _iTest.LogFolder / resultFileName;

    var dotNetTestSettings = new DotNetTestSettings()
        .SetProjectFile(testCase.Project.FilePath)
        .AddLoggers($"trx;LogFileName={resultFilePath}")
        .EnableNoRestore()
        .EnableNoBuild()
        .When(!string.IsNullOrEmpty(_iTest.TestFilter), s => s
            .SetFilter(_iTest.TestFilter)
        );

    foreach (var configure in testCase.Row.Elements.OfType<IConfigureTestSettings>())
      dotNetTestSettings = configure.ConfigureTestSettings(dotNetTestSettings);

    var testExecutionContext = new TestExecutionContext(
        _iTest,
        testCase,
        dotNetTestSettings);
    var testExecutionRuntime = testCase.TestExecutionRuntimeFactory.CreateTestExecutionRuntime(testExecutionContext);

    Action<TestExecutionContext> next = context => testExecutionRuntime.ExecuteTests(context);
    foreach (var testExecutionWrapper in testCase.TestExecutionWrappers.AsEnumerable().Reverse())
    {
      var myNext = next;
      next = context => testExecutionWrapper.ExecuteTests(context, myNext);
    }

    next(testExecutionContext);

    // For unexpected exit code we want the build to fail after all tests are executed to ensure that the error is inspected
    var exitCode = testExecutionContext.ExitCode;
    if (exitCode != 0 && exitCode != 1)
    {
      Log.Fatal($"Test execution for '{testCase.Project.Name}' with '{testCase.Row}' failed with exit code {exitCode}.");
      FatalFailure = true;
      return;
    }

    if (resultFilePath.FileExists())
    {
      var passedTests = int.Parse(XmlTasks.XmlPeekSingle(resultFilePath, "//@passed")!);
      var failedTests = int.Parse(XmlTasks.XmlPeekSingle(resultFilePath, "//@failed")!);
      var totalTests = int.Parse(XmlTasks.XmlPeekSingle(resultFilePath, "//@total")!);

      if (exitCode == 0)
      {
        Log.Information($"Test execution for '{testCase.Project.Name}' with '{testCase.Row}' succeeded. ({totalTests} tests)");
      }
      else
      {
        Log.Error($"Test execution for '{testCase.Project.Name}' with '{testCase.Row}' failed. ({failedTests}/{totalTests} failed tests)");
      }

      TeamCity.Instance?.ImportData(TeamCityImportType.mstest, resultFilePath, verbose: true, action: TeamCityNoDataPublishedAction.error);

      PassedTestCount += passedTests;
      FailedTestCount += failedTests;
      TotalTestCount += totalTests;
    }
    else
    {
      if (exitCode == 0)
      {
        Log.Warning(
            $"Test execution for '{testCase.Project.Name}' with '{testCase.Row}' did not produce an output file but reported exit code = 0. "
            + "This can be correct if the target framework is not supported or if there are no tests to execute.");
      }
      else
      {
        Log.Error($"Test execution for '{testCase.Project.Name}' with '{testCase.Row}' did not produce any outputs.");
      }
    }
  }
}