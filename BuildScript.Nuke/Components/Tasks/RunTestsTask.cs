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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Remotion.BuildScript.GenerateTestMatrix;
using Serilog;

namespace Remotion.BuildScript.Components.Tasks;

internal static class RunTestsTask
{
  internal static void LogTestMatrix (TestConfiguration[] output)
  {
    Log.Information("Test Matrix");
    output.ForEach(testConfig =>
    {
      Log.Information("{TestConfig}", testConfig.ToString());
    });
  }

  internal static void PrepareDockerImages (IReadOnlyCollection<TestConfiguration> testConfigs)
  {
    Log.Information("Started pulling Docker Images for tests supporting docker");
    testConfigs.ForEach(testConfig =>
    {
      DockerTasks.DockerPull(configurator => configurator
          .SetName(testConfig.ExecutionRuntime.DockerImage)
      );
    });
    Log.Information("Finished pulling Docker Images for tests supporting docker");
  }

  internal static int RunTest (
      TestConfiguration testConfig,
      Directories directories,
      string msBuildExtensionPackPath
  )
  {
    if (!Glob.Files(testConfig.TestAssemblyDirectoryPath, "*TestAdapter.dll").Any())
    {
      Assert.Fail(
          $"While testing {testConfig.TestAssemblyFullPath} no TestAdapter has been found. Please install the corresponding nuget package for your test framework.");
    }

    if (!string.IsNullOrEmpty(testConfig.TestSetupBuildFile))
    {
      MSBuildTasks.MSBuild(s => s
          .SetTargetPath(testConfig.TestSetupBuildFile)
          .SetProperty("MSBuildExtensionPackPath", msBuildExtensionPackPath)
          .SetProperty("BuildInParallel", false)
          .SetProperty("LogDirectory", directories.Log)
          .SetTargetPlatform(testConfig.Platform)
          .SetProperty("AppConfigFile", $"{testConfig.TestAssemblyFullPath}.config")
          .SetProperty("DatabaseSystem", testConfig.DatabaseSystem)
          .SetProperty("Browser", testConfig.Browser)
          .SetProperty("DockerImage", testConfig.ExecutionRuntime.DockerImage)
      );
    }

    var mergedTestCategoriesToExclude = testConfig.ExcludeCategories;
    var mergedTestCategoriesToInclude = testConfig.IncludeCategories;
    var testFilter =
        _testArgumentHelper.CreateDotNetTestFilter(mergedTestCategoriesToExclude, mergedTestCategoriesToInclude);
    var testName =
        _testArgumentHelper.CreateTestName(testConfig, mergedTestCategoriesToExclude, mergedTestCategoriesToInclude);
    var testResultOutputPath = $"{directories.Log / testName}.xml";

    var dotNetSettings = new Configure<DotNetTestSettings>(settings => settings
        .SetProjectFile(testConfig.TestAssemblyFullPath)
        .AddLoggers($"trx;LogFileName={testResultOutputPath}")
        .SetFilter(testFilter)
        .SetFramework(testConfig.TargetRuntimeMoniker)
        .SetRunSetting("RunConfiguration.TargetPlatform", testConfig.Platform)
        .EnableNoBuild()
        .EnableNoRestore()
        .When(TeamCity.Instance != null, settings => settings.AddTeamCityLogger())
    )(new DotNetTestSettings());
    int exitCode;

    if (testConfig.ExecutionRuntime.UseDocker)
    {
      var teamCityLoggerPath = GetTeamCityLoggerPath();
      var dotNetTestRunnerPath = DotNetTasks.DotNetPath;
      var dotNetTestFolderPath = Path.GetDirectoryName(dotNetTestRunnerPath);
      exitCode = RunProcessWithoutExitCodeCheck(CreateDockerRunSettings(
              testConfig,
              dotNetTestRunnerPath,
              dotNetTestFolderPath!,
              dotNetSettings.GetProcessArguments().RenderForExecution(),
              directories.Log
          )
          .When(TeamCity.Instance != null,
              settings => settings.AddVolume($"\"{teamCityLoggerPath}:{teamCityLoggerPath}\""))
      );
    }
    else
      exitCode = RunProcessWithoutExitCodeCheck(dotNetSettings);

    if (exitCode < 0 || !((AbsolutePath)testResultOutputPath).FileExists())
    {
      Log.Error("Error {ExitCode} occurred while running test: {TestAssemblyFullPath}", exitCode, testConfig.TestAssemblyFullPath);
      return -1;
    }

    var testTime = "0";
    var startTime = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@start");
    var endTime = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@finish");
    if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
      testTime = (DateTime.Parse(endTime) - DateTime.Parse(startTime)).ToString();
    var testPassed = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@passed");
    var testFailed = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@failed");
    var testCaseCount = XmlTasks.XmlPeekSingle(testResultOutputPath, "//@total");
    if (testFailed != null && testPassed != null && testCaseCount != null)
    {
      Log.Information(
          "Tests run {TestAssemblyFullPath}: duration {TestTime}, tests run {TestCaseCount}, tests passed {TestPassed}, tests failed {TestFailed}",
          testConfig.TestAssemblyFullPath,
          testTime,
          testCaseCount,
          testPassed,
          testFailed);
    }

    return string.IsNullOrEmpty(testFailed) ? -1 : int.Parse(testFailed);
  }

  private static readonly TestArgumentHelper _testArgumentHelper = new();

  private static AbsolutePath GetTeamCityLoggerPath ()
  {
    var teamcityPackage = NuGetPackageResolver
        .GetLocalInstalledPackage("TeamCity.Dotnet.Integration", ToolPathResolver.NuGetAssetsConfigFile)
        .NotNull("teamcityPackage != null");
    var loggerPath = teamcityPackage.Directory / "build" / "_common" / "vstest15";
    Assert.DirectoryExists(loggerPath);
    return loggerPath;
  }

  private static DockerRunSettings CreateDockerRunSettings (
      TestConfiguration testConfig,
      string runnerPath,
      string runnerFolderPath,
      string arguments,
      string logFolderPath)
  {
    return new Configure<DockerRunSettings>(settings => settings
        .EnableRm()
        .AddVolume($"{runnerFolderPath}:{runnerFolderPath}")
        .AddVolume($"{testConfig.TestAssemblyDirectoryPath}:{testConfig.TestAssemblyDirectoryPath}")
        .AddVolume($"{logFolderPath}:{logFolderPath}")
        .SetEntrypoint(runnerPath)
        .SetImage(testConfig.ExecutionRuntime.DockerImage)
        .SetArgs(arguments.Split(" ")
            .Select(param => param.Contains(":\\") ? $"\"{param}\"" : param).ToArray())
    )(new DockerRunSettings());
  }

  private static int RunProcessWithoutExitCodeCheck<T> (T settings)
      where T : ToolSettings, new()
  {
    using var process = ProcessTasks.StartProcess(settings);
    process.WaitForExit();
    return process.ExitCode;
  }
}
